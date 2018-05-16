using Serilog;
using System;
using System.Threading;
using TestParser.Sys;

namespace TestParser.Infrastructure
{
    public static class ObjectPoolExtensions
    {
        public static PolledObject<T> GetFromPoolWithRetrying<T>(this ObjectPool<T> pool, string methodName, int maxGetClientTryingCount = 3, int waitingSeconds = 1)
        {
            Guard.ArgumentNotNull(pool, nameof(pool));
            Guard.ArgumentNotEmpty(methodName, nameof(methodName));

            PolledObject<T> result = null;

            bool clientGot = false;
            int clientGetTryingCount = 0;
            do
            {
                clientGetTryingCount++;

                if (pool.GetFromPool(out result))
                {
                    clientGot = true;
                }
                else
                {
                    Log.Information("{0}: The creation limit has been exceeded for {0}, waiting next trying", methodName);

                    Thread.Sleep(TimeSpan.FromSeconds(waitingSeconds));
                }
            }
            while (!clientGot && (clientGetTryingCount < maxGetClientTryingCount));

            if (result == null)
            {
                Log.Warning("{0}: Can't get from pool after {0} trying", methodName, maxGetClientTryingCount);
            }

            return result;
        }
    }
}
