using System;

namespace Rayforge.ManagedResources.Pooling
{
    /// <summary>
    /// Simple buffer pool that returns standard leased buffers.
    /// Wraps buffers in <see cref="LeasedBuffer{Tdesc, Tbuffer}"/> when rented.
    /// </summary>
    /// <typeparam name="Tdesc">Descriptor type for buffer configuration. Must be unmanaged and implement <see cref="IEquatable{Tdesc}"/>.</typeparam>
    /// <typeparam name="Tbuffer">Type of buffer managed by the pool. Must implement <see cref="IPooledBuffer{Tdesc}"/>.</typeparam>
    public partial class LeasedBufferPool<Tdesc, Tbuffer> : LeasedBufferPoolBase<Tdesc, Tbuffer, LeasedBuffer<Tbuffer>>
        where Tbuffer : IPooledBuffer<Tdesc>
        where Tdesc : unmanaged, IEquatable<Tdesc>
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
        /// <returns>A <see cref="LeasedBuffer{Tdesc, Tbuffer}"/> representing the leased buffer.</returns>
        protected override LeasedBuffer<Tbuffer> CreateLease(Tbuffer buffer)
            => new LeasedBuffer<Tbuffer>(buffer, Return);
    }
}