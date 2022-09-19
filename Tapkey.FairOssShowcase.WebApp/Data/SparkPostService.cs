using SparkPost;
using System.Globalization;
using System.Net;
using Tapkey.FairOssShowcase.Webapp;
using Tapkey.FairOssShowcase.WebApp.Model;
using Transmission = SparkPost.Transmission;

namespace Tapkey.FairOssShowcase.WebApp.Data
{
    public class SparkPostService
    {
        private AppConfig _appConfig;
        private ILogger<SparkPostService> _logger;
        private Client _sparkPostClient;

        private const string EnLanguage = "en";
        private const string DeLanguage = "de";

        private Dictionary<string,string> _templates = new Dictionary<string, string>();

        public SparkPostService(AppConfig appConfig, ILogger<SparkPostService> logger)
        {
            _appConfig = appConfig;
            _logger = logger;
            _sparkPostClient = new Client(_appConfig.SparkPostClientId, _appConfig.SparkPostHost);
            _templates.Add(EnLanguage, _appConfig.SparkPostTemplateEn);
            _templates.Add(DeLanguage, _appConfig.SparkPostTemplateDe);
        }

        public async Task<bool> SendMessage(User user,string credentialId)
        {
            if (!_appConfig.SparkPostEnabled) return false;
            
            var transmission = new Transmission { Content = { TemplateId = GetTemplateId() } };
            transmission.Recipients.Add(new Recipient
            {
                Address = new Address { Email = user.Email }
            });

            transmission.SubstitutionData.Add("credentialId", credentialId);
            transmission.SubstitutionData.Add("user", user);

            try
            {
                SendTransmissionResponse response = await _sparkPostClient.Transmissions.Send(transmission);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("Failed to send message via SparkPost: {HttpStatusCode}", response.StatusCode);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message via SparkPost");
                return false;
            }
        }

        private string GetTemplateId()
        {
            var language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            return _templates.ContainsKey(language) ? _templates[language] : _templates[EnLanguage];
        }
    }
}
