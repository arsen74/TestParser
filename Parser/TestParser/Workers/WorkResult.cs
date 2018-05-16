using System;

namespace TestParser.Workers
{
    public class WorkResult
    {
        public string Url { get; set; }

        public int Status { get; set; }

        public int Index { get; set; }

        public int LinkCount { get; set; }

        public bool ShouldBreak { get; set; }
    }
}
