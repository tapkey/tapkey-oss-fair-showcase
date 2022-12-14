using System.Net.Http.Headers;

namespace Tapkey.FairOssShowcase.Webapp.Authorization
{
    public class OssApiHttpMessageHandler : DelegatingHandler
    {
        private readonly AppConfig _appConfig;

        public OssApiHttpMessageHandler(AppConfig appConfig)
        {
            _appConfig = appConfig;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
           HttpRequestMessage request,
           CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(AppConstants.OssApiKeyKey, _appConfig.ApiKey);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
