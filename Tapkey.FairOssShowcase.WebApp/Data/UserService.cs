using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tapkey.FairOssShowcase.Webapp;
using Tapkey.FairOssShowcase.WebApp.Model;
using Tapkey.OssApiClient;

namespace Tapkey.FairOssShowcase.WebApp.Data
{
    public class UserService
    {
        private IHttpClientFactory _httpClientFactory;
        private AppConfig _appConfig;
        private readonly ILogger _logger;
        private ZendeskService _zendeskService;
        private SparkPostService _sparkPostService;

        public UserService(IHttpClientFactory httpClientFactory, AppConfig appConfig, ZendeskService zendeskService, SparkPostService sparkpostService, ILogger<UserService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _appConfig = appConfig;
            _logger = logger;
            _zendeskService = zendeskService;
            _sparkPostService = sparkpostService;
        }

        public async Task<RegisterUserResult> RegisterUser(User user)
        {
            string error = ValidateUser(user);
            if (error != null)
            {
                _logger.LogError($"Problem while registering user: " + error);
                return new RegisterUserResult() { CredentialResult = CredentialResult.Failed };
            }
            var credentialId = GetCredentialId(user.Email);
            var credentialIdHex = Convert.ToHexString(credentialId);
            var shortenedCredentialIdHex = Utils.GetUserHexFromCredentialId(credentialId);

            if (_appConfig.ZendeskEnabled)
            {
                var zendeskTicketSucess = await _zendeskService.CreateZendeskTicket(user);
                if (zendeskTicketSucess)
                {
                    _logger.LogInformation($"Zendesk ticket for \"{credentialIdHex}\" has been created.");
                }
                else
                {
                    _logger.LogError($"There was a problem creating zendesk ticket for \"{credentialIdHex}\".");
                }
            }

            List<CredentialResult> results = new List<CredentialResult>();

            foreach (var ownerConfig in _appConfig.Configuration.OwnerConfigs)
            {
                var httpClient = _httpClientFactory.CreateClient(Utils.GetTapkeyOssApiClientId(ownerConfig.OwnerAccountId));
                var tapkeyOssApiClient = new TapkeyOssApiClient(httpClient);
                var mapIdentityResult = await MapIdentity(tapkeyOssApiClient, credentialId, user.Email, _appConfig.IdentityProviderId);
                if (mapIdentityResult == CredentialResult.Failed)
                {
                    return new RegisterUserResult() { CredentialResult = mapIdentityResult, CredentialId = shortenedCredentialIdHex };
                }

                results.Add(mapIdentityResult);

                var validity = _appConfig.Validity 
                    ?? DateTime.UtcNow.Add(_appConfig.ValidityDuration ?? TimeSpan.FromDays(1));

                var updateCredentialResult = await UpdateCredential(tapkeyOssApiClient, credentialId, ownerConfig.BoundLocks, validity, _appConfig.WeekBits, _appConfig.ValidBeforeHour, _appConfig.ValidFromHour);
                if (updateCredentialResult == CredentialResult.Failed)
                    return new RegisterUserResult() { CredentialResult = updateCredentialResult, CredentialId = shortenedCredentialIdHex };
            }

            if (_appConfig.SparkPostEnabled)
            {
                var sparkPostSuccess = await _sparkPostService.SendMessage(user, shortenedCredentialIdHex);
                if (sparkPostSuccess)
                {
                    _logger.LogInformation($"SparkPost message for \"{credentialIdHex}\" has been created.");
                }
                else
                {
                    _logger.LogError($"There was a problem creating SparkPost message for \"{credentialIdHex}\".");
                }
            }

            var callResult = results.Any(res => res == CredentialResult.AlreadyRegistered) ? CredentialResult.AlreadyRegistered : CredentialResult.Ok;
            return new RegisterUserResult() { CredentialId = shortenedCredentialIdHex, CredentialResult = callResult };
        }

        private async Task<CredentialResult> UpdateCredential(TapkeyOssApiClient tapkeyOssApiClient, byte[]? credentialId, BoundLockConfiguration[] boundLockConfig, DateTime validity, int weekBits, int validBeforeHour, int validFromHour)
        {
            var groupedBySiteId = boundLockConfig.GroupBy(blc => blc.SiteId).ToDictionary(blcs => blcs.Key, blcs => blcs.ToList());

            foreach (var siteIdLocks in groupedBySiteId)
            {
                var schedules = new List<OssSchedule>()
                {
                    new OssSchedule()
                                {
                                    Weeks = new List<OssScheduleWeek> ()
                                    {
                                        new OssScheduleWeek()
                                        {
                                            WeekBits = weekBits,
                                            Periods = new List<OssScheduleWeekPeriod>() {
                                                new OssScheduleWeekPeriod() {
                                                    ValidFrom = TimeSpan.FromHours(validFromHour),
                                                    ValidTo = TimeSpan.FromHours(validBeforeHour)
                                                }
                                            }
                                        }
                                    }
                                }
                };

                var profiles = new List<OssProfile>();
                siteIdLocks.Value.ForEach(siteIdLock =>
                {
                    profiles.Add(new OssProfile()
                    {
                        Id = siteIdLock.DoorId,
                        Schedule = 1,
                        ToggleFunction = OssProfileToggleFunction.OSS_PROFILE_TOGGLE_FUNCTION_OFF,
                        Type = OssProfileType.OSS_PROFILE_TYPE_DOOR,
                        UnlockTime = OssProfileUnlockTime.OSS_PROFILE_UNLOCK_TIME_DEFAULT
                    });
                });

                var updateCredentialRequest = new OssUpdateCredentialSyncRequest()
                {
                    UpdateCredentialRequest = new OssUpdateCredentialRequest
                    {
                        Data = new OssDataFile()
                        {
                            CredentialId = credentialId,
                            SiteId = siteIdLocks.Key,
                            Validity = validity,
                            Schedules = schedules,
                            Profiles = profiles
                        }
                    }
                };

                var updateCredentialResponse = await tapkeyOssApiClient.UpdateCredentialAsync(updateCredentialRequest);

                if (updateCredentialResponse.Status.Code != StatusCode.Ok && updateCredentialResponse.Status.Code != StatusCode.NothingToDo)
                {
                    _logger.LogError($"Update credential didn't finish successfully for {Convert.ToHexString(credentialId)}");
                    return CredentialResult.Failed;
                }

                _logger.LogInformation($"Update credential was sucessful: {Convert.ToHexString(credentialId)}");
            }

            return CredentialResult.Ok;
        }

        private async Task<CredentialResult> MapIdentity(TapkeyOssApiClient tapkeyOssApiClient, byte[] credentialId, string email, string identityProviderId)
        {
            var mapIdentityRequest = new OssMapIdentitySyncRequest()
            {
                SyncInfoRequest = new OssMapIdentityRequest()
                {
                    CredentialId = credentialId,
                    Identity = new OssIdentity()
                    {
                        IdentityProviderId = identityProviderId,
                        Identity = email
                    }
                }
            };

            var mapIdentityResponse = await tapkeyOssApiClient.MapIdentityAsync(mapIdentityRequest);

            if (mapIdentityResponse.Status.Code == StatusCode.NothingToDo)
            {
                _logger.LogWarning($"Credential is already mapped {Convert.ToHexString(credentialId)}");
                return CredentialResult.AlreadyRegistered;
            }

            if (mapIdentityResponse.Status.Code != StatusCode.Ok && mapIdentityResponse.Status.Code != StatusCode.NothingToDo)
            {
                _logger.LogError($"Mapping identity didn't finish successfully {Convert.ToHexString(credentialId)}");
                return CredentialResult.Failed;
            }

            _logger.LogInformation($"Mapping identity was sucessful: {Convert.ToHexString(credentialId)}");
            return CredentialResult.Ok;
        }

        private byte[] GetCredentialId(string valueToHash)
        {
            using (HashAlgorithm algorithm = SHA1.Create())
            {
                var hashBytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(valueToHash));
                return hashBytes.Take(10).ToArray();
            }
        }

        private string ValidateUser(User user)
        {
            if (string.IsNullOrWhiteSpace(user.FirstName)) return "first name must not be empty";
            if (string.IsNullOrWhiteSpace(user.LastName)) return "last name must not be empty";
            if (string.IsNullOrWhiteSpace(user.Company)) return "company must not be empty";
            if (string.IsNullOrWhiteSpace(user.Position)) return "position must not be empty";
            if (string.IsNullOrWhiteSpace(user.Email)) return "email must not be empty";
            if (string.IsNullOrWhiteSpace(user.PhoneNumber)) return "phone number must not be empty";
            if (!Utils.IsValidEmail(user.Email)) return "invalid email address";
            if (!Utils.IsValidPhoneNumber(user.PhoneNumber)) return "invalid phone number";
            if (string.IsNullOrWhiteSpace(user.Position)) return "position must not be empty";
            if (!user.AgreementCheck) return "user must accept data processing to proceed";

            return null;
        }
    }
}
