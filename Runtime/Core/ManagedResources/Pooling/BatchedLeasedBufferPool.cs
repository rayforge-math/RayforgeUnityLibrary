using Rayforge.ManagedResources.Abstractions;
using System;

namespace Rayforge.ManagedResources.Pooling
{
    /// <summary>
    /// A specialized buffer pool that supports batching for sequential buffers.
    /// Useful for ComputeBuffers or NativeArrays where allocations can be rounded
    /// to a batch size to reduce frequent reallocations.
    /// Wraps buffers in <see cref="BatchedLeasedBuffer{Tdesc, Tbuffer}"/> when rented.
    /// </summary>
    /// <typeparam name="Tdesc">Descriptor type describing the buffer. Must be unmanaged, implement <see cref="IEquatable{Tdesc}"/>, and <see cref="IBatchingDescriptor"/>.</typeparam>
    /// <typeparam name="Tbuffer">Type of the managed buffer (e.g., ManagedComputeBuffer). Must implement <see cref="IPooledBuffer{Tdesc}"/>.</typeparam>
    public partial class BatchedLeasedBufferPool<Tdesc, Tbuffer> : LeasedBufferPoolBase<Tdesc, Tbuffer, BatchedLeasedBuffer<Tbuffer>>
        where Tbuffer : IPooledBuffer<Tdesc>
        where Tdesc : unmanaged, IEquatable<Tdesc>, IBatchingDescriptor
    {
        /// <summary>
        /// Minimum allocation size to ensure a base buffer size.
        /// </summary>
        public int BaseSize { get; }

        /// <summary>
        /// Batch size for rounding allocations. If 0, batching is disabled.
        /// </summary>
        public int BatchSize { get; }

        /// <summary>
        /// Constructs a new batched buffer pool.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer when the pool is empty.</param>
        /// <param name="releaseFunc">Function to release a buffer permanently.</param>
        /// <param name="baseSize">Minimum base allocation size.</param>
        /// <param name="batchSize">Batch size for rounding allocations.</param>
        public BatchedLeasedBufferPool(
            BufferCreateFunc createFunc,
            BufferReleaseFunc releaseFunc,
            int baseSize = 1,
            int batchSize = 0)
            : base(createFunc, releaseFunc)
        {
            BaseSize = Math.Max(baseSize, 1);
            BatchSize = Math.Max(batchSize, 0);
        }

        /// <summary>
        /// Wraps a raw buffer in a leased buffer that automatically returns to the pool.
        /// </summary>
        /// <param name="buffer">The raw buffer to wrap.</param>
        /// <returns>A <see cref="BatchedLeasedBuffer{Tdesc, Tbuffer}"/> representing the leased buffer.</returns>
        protected override BatchedLeasedBuffer<Tbuffer> CreateLease(Tbuffer buffer)
            => new BatchedLeasedBuffer<Tbuffer>(
                buffer,
                Return,
                IsBatchedSize,
                SwapInternal
            );

        /// <summary>
        /// Computes the adjusted count based on batching rules.
        /// Rounds up to the nearest multiple of <see cref="BatchSize"/> if batching is enabled.
        /// </summary>
        /// <param name="requestedCount">The requested element count.</param>
        /// <returns>The adjusted element count according to batch settings.</returns>
        private int BatchedCount(int requestedCount)
        {
            int adjusted = Math.Max(1, Math.Max(requestedCount, BaseSize));
            if (BatchSize > 0)
                adjusted = ((adjusted + BatchSize - 1) / BatchSize) * BatchSize;
            return adjusted;
        }

        /// <summary>
        /// Checks whether the given buffer's size already matches the requested count
        /// after applying batching rules.
        /// </summary>
        /// <param name="buffer">The buffer to check.</param>
        /// <param name="count">The desired element count.</param>
        /// <returns>
        /// True if the buffer's current size corresponds exactly to the batched count; 
        /// otherwise, false.
        /// </returns>
        private bool IsBatchedSize(Tbuffer buffer, int count)
            => BatchedCount(count) == buffer.Descriptor.Count;

        /// <summary>
        /// Ensures that a buffer matches the requested count according to batching rules.
        /// If the current buffer size does not match, it is returned to the pool and a new
        /// buffer of the appropriate size is rented.
        /// </summary>
        /// <param name="buffer">The current buffer that may be resized.</param>
        /// <param name="count">The desired element count.</param>
        /// <returns>
        /// A buffer with the correct size according to batching rules. This may be the 
        /// original buffer if the size already matches, or a newly rented buffer otherwise.
        /// </returns>
        private Tbuffer SwapInternal(Tbuffer buffer, int count)
        {
            if (!IsBatchedSize(buffer, count))
            {
                var desc = buffer.Descriptor;
                Return(buffer);
                desc.Count = BatchedCount(count);
                return RentInternal(desc);
            }
            return buffer;
        }

        /// <summary>
        /// Rents a buffer internally from the pool using batch-adjusted sizing.
        /// This returns the raw buffer without wrapping it in a lease.
        /// </summary>
        /// <param name="desc">Descriptor for the buffer to rent.</param>
        /// <returns>The rented buffer instance of type <typeparamref name="Tbuffer"/>.</returns>
        protected override Tbuffer RentInternal(Tdesc desc)
        {
            desc.Count = BatchedCount(desc.Count);
            return base.RentInternal(desc);
        }

        /// <summary>
        /// Rents a buffer from the pool using batch-adjusted sizing.
        /// The buffer is wrapped in a lease of type <see cref="BatchedLeasedBuffer{Tbuffer}"/>.
        /// </summary>
        /// <param name="desc">Descriptor used to identify or create the buffer.</param>
        /// <returns>A leased buffer of type <see cref="BatchedLeasedBuffer{Tbuffer}"/>.</returns>
        public override BatchedLeasedBuffer<Tbuffer> Rent(Tdesc desc)
        {
            desc.Count = BatchedCount(desc.Count);
            return base.Rent(desc);
        }
    }
}