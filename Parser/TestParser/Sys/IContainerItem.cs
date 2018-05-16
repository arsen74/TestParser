using System;

namespace TestParser.Sys
{
    public interface IContainerItem<T> where T : IComparable<T>, IEquatable<T>
    {
        T Value { get; set; }
    }
}
