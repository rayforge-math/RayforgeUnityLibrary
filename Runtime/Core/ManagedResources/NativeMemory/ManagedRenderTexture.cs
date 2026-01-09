using Rayforge.Core.ManagedResources.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Managed wrapper around <see cref="RenderTexture"/> that ensures proper creation,
    /// configuration, and disposal. Inherits from <see cref="ManagedBuffer{TDesc, TBuffer}"/>.
    /// </summary>
    public sealed class ManagedRenderTexture : ManagedBuffer<RenderTextureDescriptorWrapper, RenderTexture>
    {
        /// <summary>
        /// Private constructor to initialize the managed render texture.
        /// Use the <see cref="Create"/> factory method instead of calling directly.
        /// </summary>
        /// <param name="texture">The internal <see cref="RenderTexture"/> to manage.</param>
        /// <param name="descriptor">Descriptor describing texture properties.</param>
        private ManagedRenderTexture(RenderTexture texture, RenderTextureDescriptorWrapper descriptor)
            : base(texture, descriptor)
        { }

        /// <summary>
        /// Creates and configures a managed render texture.
        /// </summary>
        /// <param name="desc">Descriptor defining resolution, format, and other texture properties.</param>
        /// <param name="filterMode">Filter mode (Point, Bilinear, Trilinear) for sampling.</param>
        /// <param name="wrapMode">Wrap mode (Clamp, Repeat, Mirror) for texture coordinates.</param>
        /// <returns>A new <see cref="ManagedRenderTexture"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <see cref="RenderTextureDescriptorWrapper.Width"/> or <see cref="RenderTextureDescriptorWrapper.Height"/> is <= 0.
        /// </exception>
        public static ManagedRenderTexture Create(RenderTextureDescriptorWrapper desc, FilterMode filterMode, TextureWrapMode wrapMode)
        {
            if (desc.Width <= 0)
                throw new ArgumentOutOfRangeException(nameof(desc.Width), "RenderTexture width must be greater than zero.");
            if (desc.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(desc.Height), "RenderTexture height must be greater than zero.");

            var texture = new RenderTexture(desc.Descriptor)
            {
                filterMode = filterMode,
                wrapMode = wrapMode
            };

            texture.Create();
            return new ManagedRenderTexture(texture, desc);
        }

        /// <summary>
        /// Releases the underlying GPU render texture and clears internal references.
        /// After this call, the texture is no longer valid.
        /// </summary>
        public override void Release()
        {
            if (m_Buffer != null && m_Buffer.IsCreated())
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