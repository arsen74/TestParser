using Serilog;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TestParser.Sys;

namespace TestParser.Infrastructure
{
    public class HttpClientCaller
    {
        private readonly int _maxGetClientTryingCount;
        private readonly ServiceRetryUtility _retry;

        private HttpClientPool _httpClientPool;

        public HttpClientCaller(HttpClientPool httpClientPool)
        {
            Guard.ArgumentNotNull(httpClientPool, nameof(httpClientPool));

            _httpClientPool = httpClientPool;

            _retry = new ServiceRetryUtility(TimeSpan.FromSeconds(5), 3);

            _maxGetClientTryingCount = 5;
        }

        public async Task<(HttpStatusCode Code, string Html)> GetAsync(string url, CancellationToken token = default(CancellationToken))
        {
            (HttpStatusCode code, string Html) result = default((HttpStatusCode code, string Html));

            PolledObject<HttpClient> clientInfo = null;
            try
            {
                string methodName = $"Calling GET to {url}";

                clientInfo = _httpClientPool.GetFromPoolWithRetrying(methodName, _maxGetClientTryingCount);

                if (clientInfo != null)
                {
                    bool maxRetryCountAchieved = false;
                    result = await _retry.DoWithRetryAsync(
                        job: async (n, ct) =>
                        {
                            var data = await clientInfo.Instance.GetAsync(new Uri(url))
                                .ConfigureAwait(false);

                            if (data.IsSuccessStatusCode)
                            {
                                using (var stream = new MemoryStream())
                                {
                                    await data.Content.CopyToAsync(stream);
                                    stream.Seek(0, SeekOrigin.Begin);

                                    using (var reader = new StreamReader(stream, true))
                                    {
                                        return (Code: data.StatusCode, Html: await reader.ReadToEndAsync());
                                    }
                                }
                            }
                            else
                            {
                                return (Code: data.StatusCode, Html: string.Empty);
                            }
                            
                        },
                        exceptionHandler: (ex) =>
                        {
                            _httpClientPool.CloseObject(clientInfo);

                            Log.Error(ex, "There was an exception while execute {0} with {1}", methodName, clientInfo.Id);
                        },
                        failCallback: (p) =>
                        {
                            maxRetryCountAchieved = true;

                            Log.Warning("Can't execute {0}", methodName);
                        },
                        token: token);

                    if (maxRetryCountAchieved)
                    {
                        throw new HttpClientCallerMaxRetryException($"Can't execute {methodName}, server doesn't respond after {_retry.RetryCount} attempt(s)");
                    }
                }
                else
                {
                    throw new HttpClientPoolException();
                }
            }
            finally
            {
                _httpClientPool.ReleaseToPool(clientInfo);
            }

            return result;
        }
    }
}
