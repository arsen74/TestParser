using System;

namespace TestParser.Parsing
{
    public sealed class ATagResult : BaseTagResult
    {
        public string Href { get; set; }

        public string HrefLang { get; set; }

        public string Target { get; set; }

        public string Download { get; set; }

        public string Rel { get; set; }

        public string Type { get; set; }

        public bool IsExternal { get; set; }

        public override bool IsValid
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Href);
            }
        }
    }
}
