using Rayforge.Core.ManagedResources.Abstractions;
using System;

namespace Rayforge.Core.ManagedResources.Pooling
{
    /// <summary>
    /// A specialized buffer pool that supports batching for sequential buffers.
    /// Useful for ComputeBuffers or NativeArrays where allocations can be rounded
    /// to a batch size to reduce frequent reallocations.
    /// Wraps buffers in <see cref="BatchedLeasedBuffer{TDesc, TBuffer}"/> when rented.
    /// </summary>
    /// <typeparam name="TDesc">Descriptor type describing the buffer. Must be unmanaged, implement <see cref="IEquatable{TDesc}"/>, and <see cref="IBatchingDescriptor"/>.</typeparam>
    /// <typeparam name="TBuffer">Type of the managed buffer (e.g., ManagedComputeBuffer). Must implement <see cref="IPooledBuffer{TDesc}"/>.</typeparam>
    public partial class BatchedLeasedBufferPool<TDesc, TBuffer> : LeasedBufferPoolBase<TDesc, TBuffer, BatchedLeasedBuffer<TBuffer>>
        where TBuffer : IPooledBuffer<TDesc>
        where TDesc : unmanaged, IEquatable<TDesc>, IBatchingDescriptor
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
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="baseSize"/> is less than 1 or <paramref name="batchSize"/> is negative.</exception>
        public BatchedLeasedBufferPool(
            BufferCreateFunc createFunc,
            BufferReleaseFunc releaseFunc,
            int baseSize = 1,
            int batchSize = 0)
            : base(createFunc, releaseFunc)
        {
            if (baseSize < 1)
                throw new ArgumentOutOfRangeException(nameof(baseSize), "Base size must be at least 1.");
            if (batchSize < 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size cannot be negative.");

            BaseSize = baseSize;
            BatchSize = batchSize;
        }

        /// <summary>
        /// Wraps a raw buffer in a leased buffer that automatically returns to the pool.
        /// </summary>
        /// <param name="buffer">The raw buffer to wrap.</param>
        /// <returns>A <see cref="BatchedLeasedBuffer{TDesc, TBuffer}"/> representing the leased buffer.</returns>
        protected override BatchedLeasedBuffer<TBuffer> CreateLease(TBuffer buffer)
            => new BatchedLeasedBuffer<TBuffer>(
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
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="requestedCount"/> is less than 1.</exception>
        private int BatchedCount(int requestedCount)
        {
            if (requestedCount < 1)
                throw new ArgumentOutOfRangeException(nameof(requestedCount), "Requested count must be at least 1.");

            int adjusted = Math.Max(1, Math.Max(requestedCount, BaseSize));
            if (BatchSize > 0)
                adjusted = ((adjusted + BatchSize - 1) / BatchSize) * BatchSize;
            return adjusted;
        }

        /// <summary>
        /// Checks whether the given buffer's size already matches the requested count
        /// after applying batching rules.
        /// </summary>
        /// <param name="buffer">The buffer to check. Cannot be null.</param>
        /// <param name="count">The desired element count. Must be ≥ 1.</param>
        /// <returns>
        /// True if the buffer's current size corresponds exactly to the batched count; 
        /// otherwise, false.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is less than 1.</exception>
        private bool IsBatchedSize(TBuffer buffer, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1.");

            return BatchedCount(count) == buffer.Descriptor.Count;
        }

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
        private TBuffer SwapInternal(TBuffer buffer, int count)
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
        /// </summary>
        /// <param name="desc">Descriptor for the buffer to rent. Cannot be null. Count must be ≥ 1.</param>
        /// <returns>The rented buffer instance of type <typeparamref name="TBuffer"/>.</returns>
        protected override TBuffer RentInternal(TDesc desc)
        {
            desc.Count = BatchedCount(desc.Count);
            return base.RentInternal(desc);
        }

        /// <summary>
        /// Rents a buffer from the pool using batch-adjusted sizing.
        /// The buffer is wrapped in a lease of type <see cref="BatchedLeasedBuffer{TBuffer}"/>.
        /// </summary>
        /// <param name="desc">Descriptor used to identify or create the buffer. Cannot be null. Count must be ≥ 1.</param>
        /// <returns>A leased buffer of type <see cref="BatchedLeasedBuffer{TBuffer}"/>.</returns>
        public override BatchedLeasedBuffer<TBuffer> Rent(TDesc desc)
        {
            desc.Count = BatchedCount(desc.Count);
            return base.Rent(desc);
        }
    }
}