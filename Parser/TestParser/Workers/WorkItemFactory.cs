using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TestParser.Sys;

namespace TestParser.Workers
{
    public class WorkItemFactory
    {
        private readonly Func<int, string, Task<(HttpStatusCode Code, int LinkCount)>> _action;
        private ConcurrentQueue<(int Index, string Url)> _urls;

        private int _total;
        private volatile int _current;

        public WorkItemFactory(string[] urls, Func<int, string, Task<(HttpStatusCode Code, int LinkCount)>> action)
        {
            Guard.ArgumentNotNull(urls, nameof(urls));
            Guard.ArgumentNotNull(action, nameof(action));

            _action = action;

            int i = 0;
            _urls = new ConcurrentQueue<(int Index, string Url)>(urls.Select(p => (Index: i++, Url: p)));

            _total = urls.Length;
        }

        public async Task<WorkResult> ExecuteWorkItem()
        {
            if (!_urls.TryDequeue(out (int Index, string Url) data))
            {
                return new WorkResult
                {
                    ShouldBreak = true
                };
            }

            try
            {
                Console.WriteLine($"Processing {data.Url}");

                var (Code, LinkCount) = await _action(data.Index, data.Url);

                return new WorkResult
                {
                    Status = (int)Code,
                    LinkCount = LinkCount,
                    Index = data.Index,
                    Url = data.Url
                };
            }
            finally
            {                
                Console.WriteLine($"Processed {++_current} from {_total}");
            }
        }
    }
}
