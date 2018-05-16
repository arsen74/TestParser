using System;

namespace TestParser.Infrastructure
{
    public class HttpClientCallerMaxRetryException : Exception
    {
        public HttpClientCallerMaxRetryException(string message)
            : base(message)
        { }
    }
}
