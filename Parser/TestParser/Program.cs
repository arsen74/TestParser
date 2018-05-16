using PowerArgs;
using Serilog;
using Serilog.Events;
using System;

namespace TestParser
{
    class Program
    {
        static void Main(string[] args)
        {
            //args = new[] { "ProcessUrls", "http://www.yandex.ru;http://mail.ru;http://www.google.ru" };

            Init();

            ExecuteCommand(args);

            Console.WriteLine("That's all, please press any key for exit or just close this window");
            Console.ReadLine();
        }

        private static void ExecuteCommand(string[] args)
        {
            try
            {
                Args.InvokeAction<CmdExecutor>(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void Init()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("log-parser.txt", shared: true, restrictedToMinimumLevel: LogEventLevel.Error)
                .CreateLogger();
        }
    }
}
