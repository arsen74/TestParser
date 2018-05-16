using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace TestParser
{
    public static class ParserConfiguration
    {
        private static Lazy<string> _resultFilePath = new Lazy<string>(LoadResultFilePath);
        private static Lazy<string> _listDelimiter = new Lazy<string>(LoadListDelimiter);
        private static Lazy<int> _maxExecutionTime = new Lazy<int>(LoadMaxExecutionTime);
        private static Lazy<int> _degreeOfParallelism = new Lazy<int>(LoadDegreeOfParallelism); 

        public static string ResultFilePath => _resultFilePath.Value;

        public static string ListDelimiter => _listDelimiter.Value;

        public static int MaxExecutionTime => _maxExecutionTime.Value;

        public static int DegreeOfParallelism => _degreeOfParallelism.Value;

        public static IConfigurationRoot Configuration { get; set; }

        static ParserConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();
        }

        private static string LoadResultFilePath()
        {
            return $"{Configuration["outputFile"]}" ?? "result.txt";
        }

        private static string LoadListDelimiter()
        {
            return $"{Configuration["listDelimiter"]}" ?? ";";
        }

        private static int LoadMaxExecutionTime()
        {
            return int.TryParse($"{Configuration["maxExecutionTime"]}", out int result) ?
                result :
                20;
        }

        private static int LoadDegreeOfParallelism()
        {
            return int.TryParse($"{Configuration["degreeOfParallelism"]}", out int result) ?
                result :
                4;
        }
    }
}
