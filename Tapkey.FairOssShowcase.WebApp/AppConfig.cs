using Tapkey.FairOssShowcase.WebApp.Model;

namespace Tapkey.FairOssShowcase.Webapp
{
    public class AppConfig
    {
        public string ApiKey { get; set; }
        public string TenantId { get; set; }
        public string TapkeyOssApiBaseUrl { get; set; }
        public string IdentityProviderId { get; set; }
        public Configuration Configuration { get; set; }
        public DateTime? Validity { get; set; }
        public TimeSpan? ValidityDuration { get; set; }
        public int WeekBits { get; set; }
        public int ValidFromHour { get; set; }
        public int ValidBeforeHour { get; set; }

        public bool ZendeskEnabled { get; set; }
        public string ZendeskUrl { get; set; }
        public string ZendeskApiToken { get; set; }
        public string ZendeskUser { get; set; }
        public string ZendeskLocale { get; set; }
        public long ZendeskAssigneeId { get; set; }
        public string FairTitle { get; set; }
        public bool SparkPostEnabled { get; set; }
        public string SparkPostClientId { get; set; }
        public string SparkPostHost { get; set; }
        public string SparkPostTemplateEn { get; set; }
        public string SparkPostTemplateDe { get; set; }
    }
}
