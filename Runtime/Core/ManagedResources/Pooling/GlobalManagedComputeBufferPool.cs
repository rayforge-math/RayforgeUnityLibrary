using Rayforge.ManagedResources.NativeMemory;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayforge.ManagedResources.Pooling
{
    /// <summary>
    /// Global static pool for <see cref="ManagedComputeBuffer"/> instances.
    /// </summary>
    public sealed class GlobalManagedComputeBufferPool : GlobalManagedPoolBase<ComputeBufferDescriptor, ManagedComputeBuffer>
    {
        /// <summary>
        /// Static constructor initializes the global pool.
        /// </summary>
        static GlobalManagedComputeBufferPool()
        {
            m_Pool = new LeasedBufferPool<ComputeBufferDescriptor, ManagedComputeBuffer>(
                createFunc: (desc) => new ManagedComputeBuffer(desc),
                releaseFunc: (buffer) => buffer.Dispose()
            );
        }

        /// <summary>
        /// Rents a typed compute buffer from the global pool.
        /// Automatically determines stride based on <typeparamref name="T"/>.
        /// The returned buffer is wrapped in a <see cref="LeasedBuffer{Tdesc,Tbuffer}"/> and automatically returned when disposed.
        /// </summary>
        /// <typeparam name="T">The element type stored in the compute buffer.</typeparam>
        /// <param name="count">Number of elements in the buffer.</param>
        /// <param name="type">Optional compute buffer type. Default is structured.</param>
        /// <returns>A leased buffer representing the rented <see cref="ManagedComputeBuffer"/>.</returns>
        public static LeasedBuffer<ManagedComputeBuffer> Rent<T>(int count, ComputeBufferType type = ComputeBufferType.Structured)
            where T : unmanaged
        {
            int stride = Marshal.SizeOf<T>();
            var desc = new ComputeBufferDescriptor { count = count, stride = stride, type = type };
            return m_Pool.Rent(desc);
        }
    }
}