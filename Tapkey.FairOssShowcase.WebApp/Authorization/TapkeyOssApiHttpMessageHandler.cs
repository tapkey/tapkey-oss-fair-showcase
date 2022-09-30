using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using System.Text;

namespace Tapkey.FairOssShowcase.Webapp.Authorization
{
    public class TapkeyOssApiHttpMessageHandler : DelegatingHandler
    {
        private readonly IMemoryCache _memoryCache;
        private readonly AppConfig _appConfig;

        public TapkeyOssApiHttpMessageHandler(IMemoryCache memoryCache, AppConfig appConfig)
        {
            _memoryCache = memoryCache;
            _appConfig = appConfig;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
           HttpRequestMessage request,
           CancellationToken cancellationToken)
        {
            SetApiKey(request);
            return await base.SendAsync(request, cancellationToken);
        }

        private void SetApiKey(HttpRequestMessage request)
        {
            var apiKey = _memoryCache.Get<string>(AppConstants.OssApiKeyKey);
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = GenerateApiKey();
                _memoryCache.Set(AppConstants.OssApiKeyKey, apiKey);
            }
            
            request.Headers.Authorization = new AuthenticationHeaderValue(AppConstants.OssApiKeyKey, apiKey);
        }

        private string GenerateApiKey()
        {
            var bytes = Encoding.UTF8.GetBytes($"{_appConfig.ClientId}:{_appConfig.ClientSecret}");
            string base64Credentials = Convert.ToBase64String(bytes);
            return $"Basic {base64Credentials}";
        }
    }
}
