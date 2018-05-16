using System;

namespace TestParser.Infrastructure
{
    public class HttpClientPoolException : Exception
    {
        public HttpClientPoolException()
            : base("Http client error")
        { }
    }
}
