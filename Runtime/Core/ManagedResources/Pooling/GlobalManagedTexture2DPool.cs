using Rayforge.Core.ManagedResources.NativeMemory;

namespace Rayforge.Core.ManagedResources.Pooling
{
    /// <summary>
    /// Global static access to a pool of managed Texture2D objects.
    /// Provides simple Rent() for default use.
    /// </summary>
    public sealed class GlobalManagedTexture2DPool : GlobalManagedPoolBase<Texture2dDescriptor, ManagedTexture2D>
    {
        /// <summary>
        /// Static constructor initializes the default global pool.
        /// </summary>
        static GlobalManagedTexture2DPool()
        {
            m_Pool = new LeasedBufferPool<Texture2dDescriptor, ManagedTexture2D>(
                createFunc: desc => ManagedTexture2D.Create(desc),
                releaseFunc: buffer => buffer.Release()
            );
        }
    }
}