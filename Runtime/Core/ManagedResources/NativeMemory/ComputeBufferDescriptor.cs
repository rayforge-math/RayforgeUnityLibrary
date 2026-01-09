using Rayforge.Core.Diagnostics;
using Rayforge.Core.ManagedResources.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Describes the properties of a compute buffer.
    /// Used to define element count, stride, batch padding, and buffer type.
    /// </summary>
    public struct ComputeBufferDescriptor : IEquatable<ComputeBufferDescriptor>, IBatchingDescriptor
    {
        /// <summary>Number of elements in the buffer.</summary>
        private int count;

        /// <summary>Stride in bytes per element.</summary>
        private int stride;

        /// <summary>Type of the ComputeBuffer (Default, Structured, etc.).</summary>
        private ComputeBufferType type;

        /// <summary>
        /// Number of elements requested in the buffer. 
        /// The pool may use this value along with the batch size to determine actual allocation size.
        /// </summary>
        public int Count
        {
            get => count;
            set
            {
                Assertions.AtLeastOne(value, "Count must be > 0.");
                count = value;
            }
        }

        /// <summary>Stride in bytes per element. Must be > 0.</summary>
        public int Stride
        {
            get => stride;
            set
            {
                Assertions.AtLeastOne(value, "Stride must be greater than zero.");
                stride = value;
            }
        }

        /// <summary>Type of the ComputeBuffer.</summary>
        public ComputeBufferType Type
        {
            get => type;
            set => type = value;
        }

        /// <summary>
        /// Checks equality against another <see cref="ComputeBufferDescriptor"/>.
        /// Two descriptors are considered equal if all fields match.
        /// </summary>
        public bool Equals(ComputeBufferDescriptor other)
            => count == other.count
            && stride == other.stride
            && type == other.type;


        /// <summary>
        /// Standard object equality override to ensure correct comparison behavior
        /// when stored in collections such as Dictionary or HashSet.
        /// </summary>
        public override bool Equals(object obj)
            => obj is ComputeBufferDescriptor other && Equals(other);


        /// <summary>
        /// Generates a hash code combining the descriptor fields.
        /// Ensures consistent hashing when used as dictionary keys.
        /// </summary>
        public override int GetHashCode()
            => (count, stride, type).GetHashCode();


        /// <summary>
        /// Equality operator for convenience and clarity.
        /// </summary>
        public static bool operator ==(ComputeBufferDescriptor left, ComputeBufferDescriptor right)
            => left.Equals(right);

        /// <summary>
        /// Inequality operator for convenience and clarity.
        /// </summary>
        public static bool operator !=(ComputeBufferDescriptor left, ComputeBufferDescriptor right)
            => !left.Equals(right);
    }
}