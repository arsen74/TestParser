using System;

namespace TestParser.Parsing
{
    public abstract class BaseTagResult
    {
        public int StartIndex { get; set; }

        public int EndIndex { get; set; }

        public string Html { get; set; }

        public abstract bool IsValid { get; }
    }
}
