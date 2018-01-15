using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NLog;
using Tiramisu.Entities;

namespace Tiramisu.RestApi
{
    public class HttpRequestHeaderInitializeHook : DelegatingHandler
    {
        private readonly string _apiKey;

        public HttpRequestHeaderInitializeHook(string key)
        {
            _apiKey = key;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(_apiKey))
                request.Headers.Add("api_key", "Bearer " + _apiKey);

            return base.SendAsync(request, cancellationToken);
        }
    }
}