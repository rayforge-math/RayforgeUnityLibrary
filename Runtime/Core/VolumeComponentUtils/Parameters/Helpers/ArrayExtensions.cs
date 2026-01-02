using System;

namespace Rayforge.VolumeComponentUtils.Parameters.Helpers
{
    public static class ArrayExtensions
    {
        public static int ArrayToHash<T>(this T[] array)
            where T : struct, IEquatable<T>
        {
            if (array == null || array.Length == 0)
                return 0;

            var hash = new HashCode();
            foreach (var item in array)
                hash.Add(item);
            return hash.ToHashCode();
        }
    }
}