using Rayforge.ManagedResources.NativeMemory;

namespace Rayforge.ManagedResources.Pooling
{
    /// <summary>
    /// Global static access to a pool of managed system buffers (<see cref="NativeArray{TType}"/>).
    /// Provides simple Rent() for default use,
    /// </summary>
    /// <typeparam name="TType">The struct type stored in the NativeArray.</typeparam>
    public sealed class GlobalManagedSystemBufferPool<TType> : GlobalManagedPoolBase<SystemBufferDescriptor, ManagedSystemBuffer<TType>>
        where TType : struct
    {
        /// <summary>
        /// Static constructor initializes the default global pool.
        /// </summary>
        static GlobalManagedSystemBufferPool()
        {
            m_Pool = new LeasedBufferPool<SystemBufferDescriptor, ManagedSystemBuffer<TType>>(
                createFunc: desc => new ManagedSystemBuffer<TType>(desc),
                releaseFunc: buffer => buffer.Release()
            );
        }
    }
}