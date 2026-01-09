using Rayforge.Core.ManagedResources.NativeMemory;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.Pooling
{
    /// <summary>
    /// Managed pool for <see cref="ManagedRenderTexture"/> objects.
    /// Provides default create/release functions.
    /// </summary>
    public sealed class ManagedRenderTexturePool : LeasedBufferPool<RenderTextureDescriptorWrapper, ManagedRenderTexture>
    {
        /// <summary>
        /// Default constructor using standard factory methods for managed render textures.
        /// </summary>
        public ManagedRenderTexturePool()
            : base(
                createFunc: desc => ManagedRenderTexture.Create(desc, FilterMode.Bilinear, TextureWrapMode.Clamp),
                releaseFunc: buffer => buffer.Release())
        { }

        /// <summary>
        /// Constructor allowing custom create/release functions.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer.</param>
        /// <param name="releaseFunc">Function to release a buffer permanently.</param>
        public ManagedRenderTexturePool(
            BufferCreateFunc createFunc,
            BufferReleaseFunc releaseFunc)
            : base(createFunc, releaseFunc)
        { }
    }
}