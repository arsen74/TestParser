using System;
using System.Collections.Generic;
using TestParser.Sys;

namespace TestParser.Parsing
{
    public class GenericTagResult : BaseTagResult
    {
        private readonly Dictionary<string, string> _attributes;

        public IReadOnlyDictionary<string, string> Attributes
        {
            get
            {
                return _attributes;
            }
        }

        public string RawAttributesHtml { get; set; }

        public override bool IsValid
        {
            get
            {
                return true;
            }
        }

        public GenericTagResult()
        {
            _attributes = new Dictionary<string, string>();
        }

        internal void AddAttributePair(string name, string value)
        {
            Guard.ArgumentNotEmpty(name, nameof(name));

            _attributes[name] = value;
        }
    }
}
