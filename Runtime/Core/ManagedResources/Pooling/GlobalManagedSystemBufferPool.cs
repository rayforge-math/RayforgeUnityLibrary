using Rayforge.ManagedResources.Abstraction;
using Rayforge.ManagedResources.NativeMemory;

namespace Rayforge.ManagedResources.Pooling
{
    /// <summary>
    /// Global static access to a pool of managed system buffers (<see cref="NativeArray{T}"/>).
    /// Provides simple Rent() for default use,
    /// including a batched variant for sequential buffers to reduce frequent reallocations.
    /// </summary>
    /// <typeparam name="T">The struct type stored in the NativeArray.</typeparam>
    public sealed class GlobalManagedSystemBufferPool<T> : GlobalManagedPoolBase<SystemBufferDescriptor, ManagedSystemBuffer<T>>
        where T : struct
    {
        /// <summary>
        /// Static constructor initializes the default global pool.
        /// </summary>
        static GlobalManagedSystemBufferPool()
        {
            m_Pool = new LeasedBufferPool<SystemBufferDescriptor, ManagedSystemBuffer<T>>(
                createFunc: desc => new ManagedSystemBuffer<T>(desc),
                releaseFunc: buffer => buffer.Release()
            );
        }
    }
}