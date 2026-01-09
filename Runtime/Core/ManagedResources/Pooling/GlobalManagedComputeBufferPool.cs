using Rayforge.Core.ManagedResources.NativeMemory;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.Pooling
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
                createFunc: (desc) => ManagedComputeBuffer.Create(desc),
                releaseFunc: (buffer) => buffer.Dispose()
            );
        }

        /// <summary>
        /// Rents a typed compute buffer from the global pool.
        /// Automatically determines stride based on <typeparamref name="TType"/>.
        /// The returned buffer is wrapped in a <see cref="LeasedBuffer{TDesc,TBuffer}"/> and automatically returned when disposed.
        /// </summary>
        /// <typeparam name="TType">The element type stored in the compute buffer.</typeparam>
        /// <param name="count">Number of elements in the buffer.</param>
        /// <param name="type">Optional compute buffer type. Default is structured.</param>
        /// <returns>A leased buffer representing the rented <see cref="ManagedComputeBuffer"/>.</returns>
        public static LeasedBuffer<ManagedComputeBuffer> Rent<TType>(int count, ComputeBufferType type = ComputeBufferType.Structured)
            where TType : unmanaged
        {
            int stride = Marshal.SizeOf<TType>();
            var desc = new ComputeBufferDescriptor { Count = count, Stride = stride, Type = type };
            return m_Pool.Rent(desc);
        }
    }
}