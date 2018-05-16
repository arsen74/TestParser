using System;

namespace TestParser.Infrastructure
{
    public class PolledObject<T>
    {
        public Guid Id { get; internal set; }

        public T Instance { get; internal set; }
    }
}
