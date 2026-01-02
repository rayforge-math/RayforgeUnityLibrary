using UnityEngine;
using Rayforge.ManagedResources.NativeMemory;

namespace Rayforge.ManagedResources.Pooling
{
    /// <summary>
    /// Global static access to a pool of managed render textures.
    /// Provides simple Rent() for default use.
    /// </summary>
    public sealed class GlobalManagedRenderTexturePool : GlobalManagedPoolBase<RenderTextureDescriptorWrapper, ManagedRenderTexture>
    {
        /// <summary>
        /// Static constructor initializes the default global pool.
        /// </summary>
        static GlobalManagedRenderTexturePool()
        {
            m_Pool = new LeasedBufferPool<RenderTextureDescriptorWrapper, ManagedRenderTexture>(
                createFunc: desc => new ManagedRenderTexture(desc, FilterMode.Bilinear, TextureWrapMode.Clamp),
                releaseFunc: buffer => buffer.Release()
            );
        }
    }
}