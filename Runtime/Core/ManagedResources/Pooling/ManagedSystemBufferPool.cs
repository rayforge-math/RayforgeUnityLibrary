using Rayforge.ManagedResources.NativeMemory;

namespace Rayforge.ManagedResources.Pooling
{
    /// <summary>
    /// Managed pool for system buffers (<see cref="NativeArray{T}"/>) with optional batching.
    /// Provides default create/release functions for <see cref="ManagedSystemBuffer{T}"/>.
    /// </summary>
    /// <typeparam name="T">The struct type stored in the system buffer.</typeparam>
    public sealed class ManagedSystemBufferPool<T> : BatchedLeasedBufferPool<SystemBufferDescriptor, ManagedSystemBuffer<T>>
        where T : unmanaged
    {
        /// <summary>
        /// Default constructor using standard factory methods for system buffers.
        /// </summary>
        /// <param name="baseSize">Minimum allocation size (default is 1).</param>
        /// <param name="batchSize">Batch size for rounding allocations (0 disables batching, default is 0).</param>
        public ManagedSystemBufferPool(int baseSize = 1, int batchSize = 0)
            : base(
                createFunc: desc => new ManagedSystemBuffer<T>(desc),
                releaseFunc: buffer => buffer.Release(),
                baseSize: baseSize,
                batchSize: batchSize)
        { }

        /// <summary>
        /// Constructor allowing custom create and release functions.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer.</param>
        /// <param name="releaseFunc">Function to release a buffer permanently.</param>
        /// <param name="baseSize">Minimum allocation size.</param>
        /// <param name="batchSize">Batch size for rounding allocations.</param>
        public ManagedSystemBufferPool(
            BufferCreateFunc createFunc,
            BufferReleaseFunc releaseFunc,
            int baseSize = 1,
            int batchSize = 0)
            : base(createFunc, releaseFunc, baseSize, batchSize)
        { }
    }
}