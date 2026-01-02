using Rayforge.ManagedResources.Abstraction;
using Rayforge.ManagedResources.NativeMemory;

namespace Rayforge.ManagedResources.Pooling
{
    /// <summary>
    /// Global static access to a pool of managed Texture2DArray objects.
    /// Provides Rent() for default use.
    /// </summary>
    public sealed class GlobalManagedTexture2DArrayPool : GlobalManagedPoolBase<Texture2dArrayDescriptor, ManagedTexture2DArray>
    {
        /// <summary>
        /// Static constructor initializes the default global pool.
        /// </summary>
        static GlobalManagedTexture2DArrayPool()
        {
            m_Pool = new LeasedBufferPool<Texture2dArrayDescriptor, ManagedTexture2DArray>(
                createFunc: desc => new ManagedTexture2DArray(desc),
                releaseFunc: buffer => buffer.Release()
            );
        }
    }
}