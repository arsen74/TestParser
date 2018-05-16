using System;
using System.Collections.Generic;

namespace TestParser.Parsing
{
    public class StartIndexComparer<T> : IEqualityComparer<T> where T : BaseTagResult
    {
        private static StartIndexComparer<T> _default;

        public static StartIndexComparer<T> Default
        {
            get
            {
                return _default;
            }
        }

        static StartIndexComparer()
        {
            _default = new StartIndexComparer<T>();
        }

        public bool Equals(T x, T y)
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

            return x.StartIndex.Equals(y.StartIndex);
        }

        public int GetHashCode(T obj)
        {
            return obj is null ?
                0 :
                obj.StartIndex.GetHashCode();
        }
    }
}
