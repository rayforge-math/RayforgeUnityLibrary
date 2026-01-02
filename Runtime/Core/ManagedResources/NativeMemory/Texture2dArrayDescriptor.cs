using System;

namespace Rayforge.ManagedResources.NativeMemory
{
    /// <summary>
    /// Descriptor for a 2D texture array, including the base texture descriptor
    /// and the number of array slices. Acts as a hashing key for texture array pooling.
    /// </summary>
    public struct Texture2dArrayDescriptor : IEquatable<Texture2dArrayDescriptor>
    {
        /// <summary>
        /// Descriptor that defines width, height, format and sampling settings
        /// for each texture in the array.
        /// </summary>
        public Texture2dDescriptor descriptor;

        /// <summary>
        /// Number of texture layers in the array.
        /// </summary>
        public int count;

        /// <summary>
        /// Compares both the inner descriptor and the array layer count.
        /// </summary>
        public bool Equals(Texture2dArrayDescriptor other)
            => descriptor.Equals(other.descriptor)
            && count == other.count;

        /// <summary>
        /// Ensures compatibility with object-based comparisons.
        /// </summary>
        public override bool Equals(object obj)
            => obj is Texture2dArrayDescriptor other && Equals(other);

        /// <summary>
        /// Computes a stable hash for dictionary / hash set usage.
        /// </summary>
        public override int GetHashCode()
            => (descriptor, count).GetHashCode();

        /// <summary>Equality operator.</summary>
        public static bool operator ==(Texture2dArrayDescriptor left, Texture2dArrayDescriptor right)
            => left.Equals(right);

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(Texture2dArrayDescriptor left, Texture2dArrayDescriptor right)
            => !left.Equals(right);
    }
}