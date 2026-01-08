using Rayforge.ManagedResources.Abstractions;
using Unity.Collections;

namespace Rayforge.ManagedResources.NativeMemory
{
    /// <summary>
    /// Managed wrapper around a <see cref="NativeArray{TType}"/>.
    /// Provides automatic creation, release, and pooling support.
    /// </summary>
    /// <typeparam name="TType">The struct type stored in the array.</typeparam>
    public sealed class ManagedSystemBuffer<TType> : ManagedBuffer<SystemBufferDescriptor, NativeArray<TType>>
        where TType : struct
    {
        public ManagedSystemBuffer(SystemBufferDescriptor desc)
            : base(new NativeArray<TType>(desc.count, desc.allocator), desc)
        { }

        /// <summary>
        /// Compares managed system buffers by reference.
        /// </summary>
        public override bool Equals(ManagedBuffer<SystemBufferDescriptor, NativeArray<TType>> other)
            => ReferenceEquals(this, other);

        /// <summary>
        /// Releases the underlying NativeArray memory.
        /// </summary>
        public override void Release()
            => m_Buffer.Dispose();
    }
}