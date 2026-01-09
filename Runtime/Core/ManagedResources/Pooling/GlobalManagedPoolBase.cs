using System;

namespace Rayforge.Core.ManagedResources.Pooling
{
    /// <summary>
    /// Base class providing a globally shared buffer pool for a specific 
    /// <typeparamref name="TDesc"/> / <typeparamref name="TBuffer"/> type combination.
    /// <para>
    /// For every unique set of template arguments, exactly one global buffer pool instance
    /// (<see cref="LeasedBufferPool{TDesc,TBuffer}"/>) is created and shared across all 
    /// derived classes and call sites. By using a static pool this universally holds true.
    /// </para>
    /// <para>
    /// The pooling behavior (how descriptors are generated, how buffers are matched, 
    /// and how renting/resolving works) can be fully customized by derived classes, 
    /// while the actual allocation, reuse, and lifetime management remain centralized 
    /// in the global pool.
    /// </para>
    /// <para>
    /// This ensures consistent resource reuse for each template specialization, while 
    /// still allowing flexible buffer access strategies in child classes.
    /// </para>
    /// </summary>
    public partial class GlobalManagedPoolBase<TDesc, TBuffer>
        where TBuffer : IPooledBuffer<TDesc>
        where TDesc : unmanaged, IEquatable<TDesc>
    {
        /// <summary>
        /// Internal static pool instance used for the default Rent() method.
        /// </summary>
        protected static LeasedBufferPool<TDesc, TBuffer> m_Pool;

        /// <summary>
        /// Rents a buffer from the global pool.
        /// The returned buffer is wrapped in a <see cref="LeasedBuffer{TDesc,TBuffer}"/> and automatically returned when disposed.
        /// </summary>
        /// <param name="desc">Descriptor describing the desired compute buffer.</param>
        /// <returns>A leased buffer representing the rented <see cref="ManagedComputeBuffer"/>.</returns>
        public static LeasedBuffer<TBuffer> Rent(TDesc desc)
            => m_Pool.Rent(desc);

        /// <summary>
        /// Releases all buffers in the global pool.
        /// After calling this, rented buffers will still work, 
        /// but no old buffers will be reused.
        /// </summary>
        public static void ClearUnused()
            => m_Pool.ClearUnused();

        /// <summary>
        /// Releases all buffers in the global pool.
        /// All buffers will be disposed, no matter the lease state.
        /// For <see cref="Texture2D"/>, the resource stays valid as long as a reference exists.
        /// </summary>
        public static void Dispose()
            => m_Pool.Dispose();
    }
}