using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tapkey.FairOssShowcase.Webapp;
using Tapkey.FairOssShowcase.WebApp.Model;

namespace Tapkey.FairOssShowcase.WebApp
{
    public static class Utils
    {
        public static bool IsValidEmail(string email) => new EmailAddressAttribute().IsValid(email);

        public static bool IsValidPhoneNumber(string phoneNumber) => Regex.IsMatch(phoneNumber, @"^(\+|00)?[0-9 \-\(\)\.]+$");

        public static string GetTapkeyOssApiClientId(string ownerAccountId) => $"{AppConstants.TapkeyOssApiClient}{ownerAccountId}";

        public static string GetUserHexFromCredentialId(byte[] credentialId)
        {
            var hexPartsToDisplay = credentialId.Take(4).Select(x => Convert.ToHexString(new byte[] {x})).ToArray();
            return string.Join(':', hexPartsToDisplay);
        }

        public static DateTime FloorMinutes(this DateTime t)
            => t.AddTicks(-(t.Ticks % TimeSpan.TicksPerMinute));

        public static AppConfig ParseAppConfig(ConfigurationManager configuration) 
        {
            var appConfig = new AppConfig()
            {
                ClientId = configuration.GetValue<string>("ClientId"),
                ClientSecret = configuration.GetValue<string>("ClientSecret"),
                TapkeyOssApiBaseUrl = configuration.GetValue<string>("TapkeyOssApiBaseUrl"),
                TenantId = configuration.GetValue<string>("TenantId"),
                IdentityProviderId = configuration.GetValue<string>("IdentityProviderId"),
            };

            appConfig.Validity = configuration.GetValue<DateTime?>("Validity")?.ToUniversalTime();
            appConfig.ValidityDuration = configuration.GetValue<TimeSpan?>("ValidityDuration");

            if (!int.TryParse(configuration.GetValue<string>("ValidBeforeHour"), out int validBeforeHour))
                throw new ArgumentException("ValidBeforeHour in configuration is not a number");

            if (validBeforeHour > 24)
                throw new ArgumentException("ValidBeforeHour must be <=24");

            if (!int.TryParse(configuration.GetValue<string>("ValidFromHour"), out int validFromHour))
                throw new ArgumentException("ValidFromHour in configuration is not a number");

            if (validFromHour > 24)
                throw new ArgumentException("ValidFromHour must be <=24");

            if (validFromHour >= validBeforeHour)
                 throw new ArgumentException("ValidFromHour must be smaller than ValidBeforeHour");

            appConfig.ValidFromHour = validFromHour;
            appConfig.ValidBeforeHour = validBeforeHour;

            if (!int.TryParse(configuration.GetValue<string>("WeekBits"), out int weekBits))
                throw new ArgumentException("WeekBits in configuration is not a number");
            appConfig.WeekBits = weekBits;

            if (weekBits > 127)
                throw new ArgumentException("WeekBits in configuration is invalid");

            try
            {
                appConfig.Configuration = JsonSerializer.Deserialize<Configuration>(configuration.GetValue<string>("Configuration"));
            } 
            catch (Exception ex)
            {
                throw new ArgumentException("Configuration JSON could not be parsed", ex);
            }

            appConfig.FairTitle = configuration.GetValue<string>("FairTitle");
            appConfig.ZendeskEnabled = configuration.GetValue<bool>("Zendesk_Enabled");
            appConfig.ZendeskUrl = configuration.GetValue<string>("Zendesk_Url");
            appConfig.ZendeskUser = configuration.GetValue<string>("Zendesk_User");
            appConfig.ZendeskApiToken = configuration.GetValue<string>("Zendesk_API_Token");
            appConfig.ZendeskLocale = configuration.GetValue<string>("Zendesk_Locale");

            if (!long.TryParse(configuration.GetValue<string>("Zendesk_AssigneeId"), out long zendeskAssigneeId))
                throw new ArgumentException("Zendesk_AssigneeId in configuration is not a number");

            appConfig.ZendeskAssigneeId = zendeskAssigneeId;

            appConfig.SparkPostEnabled = configuration.GetValue<bool>("SparkPost_Enabled");
            appConfig.SparkPostClientId = configuration.GetValue<string>("SparkPost_ClientId");
            appConfig.SparkPostHost = configuration.GetValue<string>("SparkPost_Host");
            appConfig.SparkPostTemplateEn = configuration.GetValue<string>("SparkPost_Template_OSSDemoGrantCreated_En");
            appConfig.SparkPostTemplateDe = configuration.GetValue<string>("SparkPost_Template_OSSDemoGrantCreated_De");

            return appConfig;
        }
    }
}
