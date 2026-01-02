using Rayforge.ManagedResources.NativeMemory;

namespace Rayforge.ManagedResources.Pooling
{
    /// <summary>
    /// Specialized managed compute buffer pool with batching support.
    /// Provides default create/destroy logic for <see cref="ManagedComputeBuffer"/> instances.
    /// Wraps buffers in <see cref="BatchedLeasedBuffer{Tdesc, Tbuffer}"/> when rented.
    /// </summary>
    public sealed class ManagedComputeBufferPool : BatchedLeasedBufferPool<ComputeBufferDescriptor, ManagedComputeBuffer>
    {
        /// <summary>
        /// Default constructor using the standard factory functions for creating and releasing buffers.
        /// Initializes the pool with optional base size and batch size for batching behavior.
        /// </summary>
        /// <param name="baseSize">Minimum allocation size (default is 1).</param>
        /// <param name="batchSize">Batch size for rounding allocations (0 disables batching, default is 0).</param>
        public ManagedComputeBufferPool(int baseSize = 1, int batchSize = 0)
            : base(
                createFunc: desc => new ManagedComputeBuffer(desc),
                releaseFunc: buffer => buffer.Dispose(),
                baseSize: baseSize,
                batchSize: batchSize)
        { }

        /// <summary>
        /// Constructor allowing custom create and release functions.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer when the pool is empty.</param>
        /// <param name="releaseFunc">Function to release a buffer permanently.</param>
        /// <param name="baseSize">Minimum allocation size for batching.</param>
        /// <param name="batchSize">Batch size for rounding allocations.</param>
        public ManagedComputeBufferPool(
            BufferCreateFunc createFunc,
            BufferReleaseFunc releaseFunc,
            int baseSize = 1,
            int batchSize = 0)
            : base(createFunc, releaseFunc, baseSize, batchSize)
        { }
    }
}