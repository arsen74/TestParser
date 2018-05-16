using System;
using System.Threading;
using System.Threading.Tasks;
using TestParser.Sys;

namespace TestParser.Infrastructure
{
    public class ServiceRetryUtility
    {
        private static readonly TimeSpan _minimumBackoffPeriod = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan _maximumBackoffInterval = TimeSpan.FromMinutes(1);

        private readonly TimeSpan _period;
        private TimeSpan? _maxExecutionPeriod;
        private int _maxRetryCount;

        /// <summary>
        /// Max possible execution period - with max retry trying
        /// </summary>
        public TimeSpan MaxExecutionPeriod
        {
            get
            {
                if (!_maxExecutionPeriod.HasValue)
                {
                    _maxExecutionPeriod = CalculateMaxtRetryTime();
                }

                return _maxExecutionPeriod.Value;
            }
        }

        public int RetryCount => _maxRetryCount;

        public ServiceRetryUtility(TimeSpan period)
            : this(period, 5)
        { }

        public ServiceRetryUtility(TimeSpan period, int maxRetryCount)
        {
            if (period < TimeSpan.Zero)
            {
                _period = _minimumBackoffPeriod;
            }
            else
            {
                _period = period;
            }

            _maxRetryCount = maxRetryCount;
        }

        public TResult DoWithRetry<TResult>(Func<int, TResult> job, Action<Exception> exceptionHandler, Action<int> failCallback)
        {
            Guard.ArgumentNotNull(job, nameof(job));
            Guard.ArgumentNotNull(exceptionHandler, nameof(exceptionHandler));
            Guard.ArgumentNotNull(failCallback, nameof(failCallback));

            TResult result = default(TResult);

            int failuresCount = 0;
            for (int i = 0; i < _maxRetryCount; i++)
            {
                try
                {
                    result = job(i);

                    failuresCount = 0;
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failuresCount++;

                    exceptionHandler(ex);
                }

                if (failuresCount == 0)
                {
                    break;
                }
                else
                {
                    Thread.Sleep(CalculateNextRetryTime(failuresCount));
                }
            }

            if (failuresCount > 0)
            {
                failCallback(failuresCount);
            }

            return result;
        }

        public async Task<TResult> DoWithRetryAsync<TResult>(Func<int, CancellationToken, Task<TResult>> job, Action<Exception> exceptionHandler, Action<int> failCallback)
        {
            return await DoWithRetryAsync(job, exceptionHandler, failCallback, CancellationToken.None)
                .ConfigureAwait(false);
        }

        public async Task<TResult> DoWithRetryAsync<TResult>(Func<int, CancellationToken, Task<TResult>> job, Action<Exception> exceptionHandler, Action<int> failCallback, CancellationToken token)
        {
            Guard.ArgumentNotNull(job, nameof(job));
            Guard.ArgumentNotNull(exceptionHandler, nameof(exceptionHandler));
            Guard.ArgumentNotNull(failCallback, nameof(failCallback));

            TResult result = default(TResult);

            int failuresCount = 0;
            for (int i = 0; i < _maxRetryCount; i++)
            {
                try
                {
                    result = await job(i, token)
                        .ConfigureAwait(false);

                    failuresCount = 0;
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failuresCount++;

                    exceptionHandler(ex);
                }

                if (failuresCount == 0)
                {
                    break;
                }
                else
                {
                    await Task.Delay(CalculateNextRetryTime(failuresCount));
                }
            }

            if (failuresCount > 0)
            {
                failCallback(failuresCount);
            }

            return result;
        }

        private TimeSpan CalculateNextRetryTime(int failuresCount)
        {
            // Available, and first failure, just try the batch interval
            if (failuresCount <= 1)
            {
                return _period;
            }

            // Second failure, start ramping up the interval
            var backoffFactor = Math.Pow(2, (failuresCount - 1));

            // If the period is ridiculously short, give it a boost so we get some
            // visible backoff
            var backoffPeriod = Math.Max(_period.Ticks, _minimumBackoffPeriod.Ticks);

            // The "ideal" interval
            var backedOff = (long)(backoffPeriod * backoffFactor);

            // Capped to the maximum interval
            var cappedBackoff = Math.Min(_maximumBackoffInterval.Ticks, backedOff);

            // Unless that's shorter than the period, in which case we'll just apply the period
            var actual = Math.Max(_period.Ticks, cappedBackoff);

            return TimeSpan.FromTicks(actual);
        }

        private TimeSpan CalculateMaxtRetryTime()
        {
            var result = TimeSpan.Zero;

            for (int i = 0; i < _maxRetryCount; i++)
            {
                result.Add(CalculateNextRetryTime(i));
            }

            return result;
        }
    }
}
