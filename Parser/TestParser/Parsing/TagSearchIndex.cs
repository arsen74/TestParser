using System;
using System.Collections.Generic;
using TestParser.Sys;

namespace TestParser.Parsing
{
    public class TagSearchIndex: IContainerItem<int>
    {
        private static IEqualityComparer<TagSearchIndex> _defaultComparer;

        static TagSearchIndex()
        {
            _defaultComparer = new TagSearchIndexComparer();
        }

        public static IEqualityComparer<TagSearchIndex> DefaultComparer
        {
            get
            {
                return _defaultComparer;
            }
        }

        public bool IsOpeningTag { get; set; }

        public bool IsClosingTag
        {
            get
            {
                return !IsOpeningTag;
            }
        }

        public int Value { get; set; }

        private class TagSearchIndexComparer : IEqualityComparer<TagSearchIndex>
        {
            public bool Equals(TagSearchIndex x, TagSearchIndex y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null && y is null)
                {
                    return true;
                }

                if (x is null)
                {
                    return false;
                }

                if (y is null)
                {
                    return false;
                }

                return x.Value.Equals(y.Value);
            }

            public int GetHashCode(TagSearchIndex obj)
            {
                return obj is null ?
                    0 :
                    obj.Value.GetHashCode();
            }
        }
    }
}
