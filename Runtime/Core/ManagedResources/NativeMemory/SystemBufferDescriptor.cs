using Rayforge.ManagedResources.Abstractions;
using System;
using Unity.Collections;

namespace Rayforge.ManagedResources.NativeMemory
{
    /// <summary>
    /// Descriptor for a native system buffer (NativeArray), including size and allocator.
    /// </summary>
    public struct SystemBufferDescriptor : IEquatable<SystemBufferDescriptor>, IBatchingDescriptor
    {
        /// <summary>Number of elements in the buffer.</summary>
        public int count;

        /// <summary>Allocator used for the NativeArray.</summary>
        public Allocator allocator;

        /// <summary>
        /// Number of elements requested in the buffer. 
        /// The pool may use this value along with the batch size to determine actual allocation size.
        /// </summary>
        public int Count
        {
            get => count;
            set => count = value;
        }

        /// <summary>
        /// Compares two descriptors for equality.
        /// </summary>
        public bool Equals(SystemBufferDescriptor other)
            => count == other.count && allocator == other.allocator;

        /// <summary>
        /// Overrides object.Equals to match IEquatable implementation.
        /// </summary>
        public override bool Equals(object obj)
            => obj is SystemBufferDescriptor other && Equals(other);

        /// <summary>
        /// Provides a hash code for use in dictionaries or hash sets.
        /// </summary>
        public override int GetHashCode()
            => (count, allocator).GetHashCode();

        /// <summary>Equality operator.</summary>
        public static bool operator ==(SystemBufferDescriptor lhs, SystemBufferDescriptor rhs)
            => lhs.Equals(rhs);

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(SystemBufferDescriptor lhs, SystemBufferDescriptor rhs)
            => !lhs.Equals(rhs);
    }
}