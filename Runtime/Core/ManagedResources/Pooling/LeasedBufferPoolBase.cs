using Rayforge.Core.Diagnostics;
using System;
using System.Collections.Generic;

namespace Rayforge.Core.ManagedResources.Pooling
{
    /// <summary>
    /// Base class for buffer pools that manage reusable buffer objects and hand them out as lease wrappers.
    /// This pool is *not* thread-safe; derived classes must implement any required thread-safety, eviction policy, 
    /// or automatic cleanup behavior.
    ///
    /// Provides mechanisms for:
    /// - Renting a buffer as a lease object of type <typeparamref name="TLease"/>.
    /// - Returning buffers to the pool.
    /// - Creating new buffers directly without leasing (useful for batch resizing or hot-swapping).
    /// </summary>
    /// <typeparam name="TDesc">Descriptor type used to categorize buffers. Must be unmanaged and implement <see cref="IEquatable{TDesc}"/>.</typeparam>
    /// <typeparam name="TBuffer">The pooled buffer type, must implement <see cref="IPooledBuffer{TDesc}"/>.</typeparam>
    /// <typeparam name="TLease">The lease wrapper type returned by the pool, must inherit from <see cref="LeasedBufferBase{TBuffer}"/>.</typeparam>
    public abstract class LeasedBufferPoolBase<TDesc, TBuffer, TLease> : IDisposable
        where TBuffer : IPooledBuffer<TDesc>
        where TDesc : unmanaged, IEquatable<TDesc>
        where TLease : LeasedBuffer<TBuffer>
    {
        /// <summary>
        /// Factory used to create new buffers on demand.
        /// </summary>
        protected readonly BufferCreateFunc m_CreateFunc;

        /// <summary>
        /// Factory used to permanently release a buffer.
        /// </summary>
        protected readonly BufferReleaseFunc m_ReleaseFunc;

        /// <summary>
        /// Free buffers grouped by descriptor for quick reuse.
        /// </summary>
        protected readonly Dictionary<TDesc, Stack<TBuffer>> m_FreeDict = new();

        /// <summary>
        /// Buffers currently leased out to consumers.
        /// </summary>
        protected readonly HashSet<TBuffer> m_Reserved = new();

        /// <summary>
        /// Factory to create a new buffer from a descriptor.
        /// </summary>
        public delegate TBuffer BufferCreateFunc(TDesc desc);

        /// <summary>
        /// Callback invoked when a buffer is permanently released from the pool.
        /// </summary>
        public delegate void BufferReleaseFunc(TBuffer buffer);

        /// <summary>
        /// Constructs a new base buffer pool with the provided create and release functions.
        /// </summary>
        /// <param name="createFunc">Function used to create a new buffer when the pool has no free buffers.</param>
        /// <param name="releaseFunc">Function used to permanently release a buffer when the pool is cleared or disposed.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="createFunc"/> or <paramref name="releaseFunc"/> is null.
        /// </exception>
        protected LeasedBufferPoolBase(BufferCreateFunc createFunc, BufferReleaseFunc releaseFunc)
        {
            m_CreateFunc = createFunc ?? 
                throw new ArgumentNullException(nameof(createFunc));
            m_ReleaseFunc = releaseFunc ?? 
                throw new ArgumentNullException(nameof(releaseFunc));
        }

        /// <summary>
        /// Derived pools must implement how the lease wrapper is constructed from a raw buffer.
        /// This method is used internally by <see cref="Rent"/> to wrap a pooled buffer in a lease object of type <typeparamref name="TLease"/>.
        /// </summary>
        /// <param name="buffer">The raw buffer to wrap in a lease.</param>
        /// <returns>A lease object of type <typeparamref name="TLease"/> that wraps the given buffer.</returns>
        protected abstract TLease CreateLease(TBuffer buffer);

        /// <summary>
        /// Rents a buffer internally from the pool. If no free buffer exists, a new one is created.
        /// This method returns the raw buffer without wrapping it in a lease.
        /// </summary>
        /// <param name="desc">Descriptor for the buffer to rent.</param>
        /// <returns>The rented buffer instance of type <typeparamref name="TBuffer"/>.</returns>
        protected virtual TBuffer RentInternal(TDesc desc)
        {
            TBuffer buffer;

            if (m_FreeDict.TryGetValue(desc, out var stack) && stack.Count > 0)
            {
                buffer = stack.Pop();
            }
            else
            {
                buffer = m_CreateFunc.Invoke(desc);
            }

            m_Reserved.Add(buffer);
            return buffer;
        }

        /// <summary>
        /// Rents a buffer from the pool. If no free buffer exists, a new one is created.
        /// The buffer is wrapped in a lease object of type <typeparamref name="TLease"/> via <see cref="CreateLease"/>.
        /// </summary>
        /// <param name="desc">Descriptor used to identify or create the buffer.</param>
        /// <returns>A lease object wrapping the rented buffer.</returns>
        public virtual TLease Rent(TDesc desc)
        {
            var buffer = RentInternal(desc);
            return CreateLease(buffer);
        }

        /// <summary>
        /// Called by a lease when its buffer is returned to the pool.
        /// Adds the buffer back into the free collection for reuse.
        /// </summary>
        /// <param name="buffer">The buffer being returned.</param>
        protected virtual void Return(TBuffer buffer)
        {
            var leased = m_Reserved.Remove(buffer);
            Assertions.IsTrue(leased, $"Attempted to return a buffer that is not currently reserved: {buffer}");

            if (!leased)
                return;

            if (!m_FreeDict.TryGetValue(buffer.Descriptor, out var stack))
            {
                stack = new Stack<TBuffer>();
                m_FreeDict[buffer.Descriptor] = stack;
            }

            stack.Push(buffer);
        }

        /// <summary>
        /// Releases all buffers in both free and reserved collections.
        /// After this call, the pool is empty and cannot be reused until new buffers are created.
        /// </summary>
        public virtual void Dispose()
        {
            foreach (var stack in m_FreeDict.Values)
                foreach (var buffer in stack)
                    m_ReleaseFunc.Invoke(buffer);

            foreach (var buffer in m_Reserved)
                m_ReleaseFunc.Invoke(buffer);

            m_FreeDict.Clear();
            m_Reserved.Clear();
        }

        /// <summary>
        /// Permanently releases all unused buffers without affecting those currently leased.
        /// This allows clearing memory pressure while keeping active leases valid.
        /// </summary>
        public virtual void ClearUnused()
        {
            foreach (var stack in m_FreeDict.Values)
                foreach (var buffer in stack)
                    m_ReleaseFunc.Invoke(buffer);

            m_FreeDict.Clear();
        }
    }
}