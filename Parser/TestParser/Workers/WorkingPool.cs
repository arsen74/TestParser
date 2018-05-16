using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using TestParser.Sys;

namespace TestParser.Workers
{
    internal class WorkingPool
    {
        private readonly WorkItemFactory _workItemFactory;
        private readonly CancellationToken _cancellationToken;
        private readonly Task[] _taskPools;
        private readonly int _degreeOfParallelism;

        public event EventHandler<WorkItemFinishedEventArgs> WorkItemFinished;

        public WorkingPool(WorkItemFactory workItemFactory, int degreeOfParallelism, CancellationToken token = default(CancellationToken))
        {
            Guard.ArgumentNotNull(workItemFactory, nameof(workItemFactory));
            Guard.ArgumentNotNull(token, nameof(token));

            _workItemFactory = workItemFactory;
            _cancellationToken = token;
            _degreeOfParallelism = degreeOfParallelism;

            _taskPools = new Task[_degreeOfParallelism];
        }

        public Task RunAsync()
        {
            for (int i = 0; i < _degreeOfParallelism; i++)
            {
                _taskPools[i] = Task.Factory.StartNew(LoopAsync, TaskCreationOptions.LongRunning).Unwrap();
            }

            Task.WaitAll(_taskPools, _cancellationToken);

            return Task.CompletedTask;
        }

        private async Task LoopAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var workItem = _workItemFactory.ExecuteWorkItem();
                    var result = await workItem;

                    if (result.ShouldBreak)
                    {
                        break;
                    }

                    OnWorkItemFinished(new WorkItemFinishedEventArgs(result));
                }
                catch(Exception ex)
                {
                    Log.Warning(ex, "An exception was happened while parsing");
                }
            }
        }

        private void OnWorkItemFinished(WorkItemFinishedEventArgs eventArgs)
        {
            WorkItemFinished?.Invoke(this, eventArgs);
        }
    }
}
