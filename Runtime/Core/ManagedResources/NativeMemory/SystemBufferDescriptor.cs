using Rayforge.Core.Diagnostics;
using Rayforge.Core.ManagedResources.Abstractions;
using System;
using Unity.Collections;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Descriptor for a native system buffer (NativeArray), including size and allocator.
    /// </summary>
    public struct SystemBufferDescriptor : IEquatable<SystemBufferDescriptor>, IBatchingDescriptor
    {
        private int count;
        private Allocator allocator;

        /// <summary>
        /// Number of elements in the buffer. Must be > 0.
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
        /// Allocator used for the NativeArray.
        /// </summary>
        public Allocator Allocator
        {
            get => allocator;
            set => allocator = value;
        }

        /// <summary>
        /// Compares two descriptors for equality.
        /// </summary>
        public bool Equals(SystemBufferDescriptor other)
            => Count == other.Count && Allocator == other.Allocator;

        /// <summary>
        /// Overrides object.Equals to match IEquatable implementation.
        /// </summary>
        public override bool Equals(object obj)
            => obj is SystemBufferDescriptor other && Equals(other);

        /// <summary>
        /// Provides a hash code for use in dictionaries or hash sets.
        /// </summary>
        public override int GetHashCode()
            => (Count, Allocator).GetHashCode();

        public static bool operator ==(SystemBufferDescriptor lhs, SystemBufferDescriptor rhs)
            => lhs.Equals(rhs);

        public static bool operator !=(SystemBufferDescriptor lhs, SystemBufferDescriptor rhs)
            => !lhs.Equals(rhs);
    }
}