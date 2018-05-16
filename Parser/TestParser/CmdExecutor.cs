using PowerArgs;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TestParser.Infrastructure;
using TestParser.Parsing;
using TestParser.Sys;
using TestParser.Workers;

namespace TestParser
{
    internal class CmdExecutor
    {
        private HttpClientCaller _client;

        [HelpHook, ArgShortcut("-?"), ArgDescription("Help")]
        public bool Help { get; set; }

        [ArgActionMethod, ArgDescription("Count links on the pages")]
        public void ProcessUrls([ArgDescription("List of urls")]string urls)
        {
            Guard.ArgumentNotEmpty(urls, nameof(urls));

            var source = urls.Split(ParserConfiguration.ListDelimiter, StringSplitOptions.RemoveEmptyEntries);

            if (source.Length == 0)
            {
                Console.WriteLine("List of urls is empty");

                return;
            }

            var filePath = ParserConfiguration.ResultFilePath;
            if (string.IsNullOrWhiteSpace(Path.GetDirectoryName(filePath)))
            {
                filePath = Path.Combine(Environment.CurrentDirectory, filePath);
            }

            var streamWriter = File.AppendText(filePath);

            _client = new HttpClientCaller(new HttpClientPool(source.Length));

            var tokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(ParserConfiguration.MaxExecutionTime));

            Task.WhenAny(
                RunAsync(
                    source,
                    streamWriter,
                    (index, url) =>
                    {
                        return UrlProcessorAsync(url, index);
                    },
                    tokenSource.Token),
                Task.Run(
                    () =>
                    {
                        Console.WriteLine("Please press any key to terminate...");

                        Console.ReadKey(true);

                        tokenSource.Cancel();
                    },
                    tokenSource.Token))
                .GetAwaiter()
                .GetResult();

            streamWriter.Dispose();
        }

        private async Task<(HttpStatusCode Code, int LinkCount)> UrlProcessorAsync(string url, int index)
        {
            var (Code, Html) = await _client.GetAsync(url);

            if (Code == HttpStatusCode.OK)
            {
                return (Code: Code, LinkCount: SearchEngine.FindLinks(Html).Count(p => p.IsExternal));
            }
            else
            {
                Log.Error("{0} returns {1}", url, Code);
            }

            return (Code: Code, LinkCount: 0);
        }

        private async Task RunAsync(string[] urls, StreamWriter fileWriter, Func<int, string, Task<(HttpStatusCode Code, int LinkCount)>> action, CancellationToken token)
        {
            var customThreadPool = new WorkingPool(
                new WorkItemFactory(urls, action), 
                urls.Length < ParserConfiguration.DegreeOfParallelism ? 
                    urls.Length :
                    ParserConfiguration.DegreeOfParallelism, 
                token);

            customThreadPool.WorkItemFinished += (sender, args) =>
            {
                fileWriter.WriteLine($"{args.Result.Url} - {args.Result.LinkCount} external links");
            };

            await customThreadPool.RunAsync();
        }
    }
}
