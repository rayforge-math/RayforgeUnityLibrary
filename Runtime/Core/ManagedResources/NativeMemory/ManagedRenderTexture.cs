using Rayforge.ManagedResources.Abstractions;
using UnityEngine;

namespace Rayforge.ManagedResources.NativeMemory
{
    /// <summary>
    /// Managed wrapper around <see cref="RenderTexture"/> that ensures proper creation,
    /// configuration, and disposal. Inherits from <see cref="ManagedBuffer{TDesc, TBuffer}"/>.
    /// </summary>
    public sealed class ManagedRenderTexture : ManagedBuffer<RenderTextureDescriptorWrapper, RenderTexture>
    {
        /// <summary>
        /// Creates and configures a managed render texture.
        /// </summary>
        /// <param name="desc">Descriptor defining resolution, format, and other texture properties.</param>
        /// <param name="filterMode">Filter mode (Point, Bilinear, Trilinear) for sampling.</param>
        /// <param name="wrapMode">Wrap mode (Clamp, Repeat, Mirror) for texture coordinates.</param>
        public ManagedRenderTexture(RenderTextureDescriptorWrapper desc, FilterMode filterMode, TextureWrapMode wrapMode)
            : base(CreateAndConfigureTexture(desc, filterMode, wrapMode), desc)
        { }

        /// <summary>
        /// Creates the <see cref="RenderTexture"/> from the descriptor and applies filtering and wrapping.
        /// </summary>
        private static RenderTexture CreateAndConfigureTexture(RenderTextureDescriptorWrapper desc, FilterMode filterMode, TextureWrapMode wrapMode)
        {
            var texture = new RenderTexture(desc.descriptor)
            {
                filterMode = filterMode,
                wrapMode = wrapMode
            };
            texture.Create();
            return texture;
        }

        /// <summary>
        /// Releases the underlying GPU render texture and clears internal references.
        /// After this call, the texture is no longer valid.
        /// </summary>
        public override void Release()
        {
            if (m_Buffer != null)
            {
                m_Buffer.Release();
                m_Buffer = null;
            }
        }

        /// <summary>
        /// Compares managed render textures by reference. Useful for pooling or resource tracking.
        /// </summary>
        public override bool Equals(ManagedBuffer<RenderTextureDescriptorWrapper, RenderTexture> other)
            => ReferenceEquals(this, other);
    }
}