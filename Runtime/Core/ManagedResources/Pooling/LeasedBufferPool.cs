using System;

namespace Rayforge.Core.ManagedResources.Pooling
{
    /// <summary>
    /// Simple buffer pool that returns standard leased buffers.
    /// Wraps buffers in <see cref="LeasedBuffer{TDesc, TBuffer}"/> when rented.
    /// </summary>
    /// <typeparam name="TDesc">Descriptor type for buffer configuration. Must be unmanaged and implement <see cref="IEquatable{TDesc}"/>.</typeparam>
    /// <typeparam name="TBuffer">Type of buffer managed by the pool. Must implement <see cref="IPooledBuffer{TDesc}"/>.</typeparam>
    public partial class LeasedBufferPool<TDesc, TBuffer> : LeasedBufferPoolBase<TDesc, TBuffer, LeasedBuffer<TBuffer>>
        where TBuffer : IPooledBuffer<TDesc>
        where TDesc : unmanaged, IEquatable<TDesc>
    {
        /// <summary>
        /// Creates a new leased buffer pool.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer when the pool is empty.</param>
        /// <param name="releaseFunc">Function to permanently release a buffer.</param>
        public LeasedBufferPool(BufferCreateFunc createFunc, BufferReleaseFunc releaseFunc)
            : base(createFunc, releaseFunc) { }

        /// <summary>
        /// Wraps a raw buffer in a leased buffer that automatically returns to the pool on disposal.
        /// </summary>
        /// <param name="buffer">The raw buffer to wrap.</param>
        /// <returns>A <see cref="LeasedBuffer{TDesc, TBuffer}"/> representing the leased buffer.</returns>
        protected override LeasedBuffer<TBuffer> CreateLease(TBuffer buffer)
            => new LeasedBuffer<TBuffer>(buffer, Return);
    }
}