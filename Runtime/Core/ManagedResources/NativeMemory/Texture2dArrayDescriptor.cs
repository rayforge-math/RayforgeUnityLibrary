using Rayforge.Core.Diagnostics;
using System;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Descriptor for a 2D texture array, including the base texture descriptor
    /// and the number of array slices. Acts as a hashing key for texture array pooling.
    /// </summary>
    public struct Texture2dArrayDescriptor : IEquatable<Texture2dArrayDescriptor>
    {
        private Texture2dDescriptor descriptor;
        private int count;

        /// <summary>
        /// Descriptor that defines width, height, format and sampling settings
        /// for each texture in the array.
        /// </summary>
        public Texture2dDescriptor Descriptor
        {
            get => descriptor;
            set => descriptor.CopyFrom(value);
        }

        /// <summary>
        /// Number of texture layers in the array. Must be > 0.
        /// </summary>
        public int Count
        {
            get => count;
            set
            {
                Assertions.AtLeastOne(value, "Count must be greater than zero.");
                count = value;
            }
        }

        /// <summary>
        /// Copies all fields from another descriptor, applying assertions.
        /// </summary>
        public void CopyFrom(Texture2dArrayDescriptor other)
        {
            Descriptor = other.Descriptor; // Assertion triggers if invalid
            Count = other.Count;           // Assertion triggers if <= 0
        }

        /// <summary>
        /// Compares both the inner descriptor and the array layer count.
        /// </summary>
        public bool Equals(Texture2dArrayDescriptor other)
            => descriptor.Equals(other.descriptor) && count == other.count;

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

        public static bool operator ==(Texture2dArrayDescriptor left, Texture2dArrayDescriptor right)
            => left.Equals(right);

        public static bool operator !=(Texture2dArrayDescriptor left, Texture2dArrayDescriptor right)
            => !left.Equals(right);
    }
}