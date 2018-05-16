using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace TestParser.Infrastructure
{
    public class HttpClientPool : ObjectPool<HttpClient>
    {
        public HttpClientPool(int maxCount)
            : this(TimeSpan.FromSeconds(45), int.MaxValue, maxCount)
        { }

        public HttpClientPool(TimeSpan responseTimeout, int maxConnections, int maxCount)
            : base("HttpClient", () => CreateClient(responseTimeout, maxConnections), CloseClient, maxCount)
        { }

        private static HttpClient CreateClient(TimeSpan responseTimeout, int maxConnections)
        {
            var result = new HttpClient(
                new HttpClientHandler
                {
                    MaxConnectionsPerServer = maxConnections
                })
            {
                Timeout = responseTimeout
            };

            result.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

            return result;
        }

        private static void CloseClient(HttpClient client)
        {
            client.CancelPendingRequests();

            client.Dispose();
        }
    }
}
