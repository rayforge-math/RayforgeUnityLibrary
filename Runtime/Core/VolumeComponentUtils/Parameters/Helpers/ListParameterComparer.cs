using System;
using System.Collections.Generic;

namespace Rayforge.VolumeComponentUtils.Parameters.Helpers
{
    public class ListParameterComparer<T> : IEqualityComparer<T>
        where T : IEquatable<T>
    {
        public bool Equals(T a, T b)
            => a.Equals(b);

        public int GetHashCode(T obj)
        {
            unchecked
            {
                return obj.GetHashCode();
            }
        }
    }
}