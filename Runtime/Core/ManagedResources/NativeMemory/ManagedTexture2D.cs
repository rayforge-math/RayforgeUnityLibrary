using Rayforge.Core.ManagedResources.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Managed wrapper around Unity's <see cref="Texture2D"/>.
    /// Provides creation, configuration, and controlled release for pooling or resource tracking.
    /// Inherits from <see cref="ManagedBuffer{TBuffer,TDesc}"/>.
    /// </summary>
    public sealed class ManagedTexture2D : ManagedBuffer<Texture2dDescriptor, Texture2D>
    {
        /// <summary>Width of the texture.</summary>
        public int Width => m_Descriptor.Width;

        /// <summary>Height of the texture.</summary>
        public int Height => m_Descriptor.Height;

        /// <summary>
        /// Private constructor used internally to wrap an existing <see cref="Texture2D"/> and descriptor.
        /// </summary>
        /// <param name="texture">The internal Texture2D resource.</param>
        /// <param name="descriptor">Descriptor defining the texture properties.</param>
        private ManagedTexture2D(Texture2D texture, Texture2dDescriptor descriptor)
            : base(texture, descriptor)
        { }

        /// <summary>
        /// Creates a managed Texture2D from the given descriptor.
        /// Validates the descriptor before allocation.
        /// </summary>
        /// <param name="desc">Descriptor describing resolution, format, mipmap settings, and filtering.</param>
        /// <returns>A new <see cref="ManagedTexture2D"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <see cref="Texture2dDescriptor.Width"/> or <see cref="Texture2dDescriptor.Height"/> is less than 1.
        /// </exception>
        public static ManagedTexture2D Create(Texture2dDescriptor desc)
        {
            if (desc.Width <= 0)
                throw new ArgumentOutOfRangeException(nameof(desc.Width), "Texture width must be greater than zero.");
            if (desc.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(desc.Height), "Texture height must be greater than zero.");

            var texture = new Texture2D(
                desc.Width,
                desc.Height,
                desc.ColorFormat,
                desc.MipCount,
                desc.Linear)
            {
                filterMode = desc.FilterMode,
                wrapMode = desc.WrapMode
            };

            return new ManagedTexture2D(texture, desc);
        }

        /// <summary>
        /// Releases the underlying texture. After this call, the texture is no longer valid.
        /// </summary>
        public override void Release()
        {
            if (m_Buffer != null)
            {
                UnityEngine.Object.Destroy(m_Buffer);
                m_Buffer = null;
            }
        }

        /// <summary>
        /// Compares managed textures by reference. Suitable for pooling or tracking.
        /// </summary>
        public override bool Equals(ManagedBuffer<Texture2dDescriptor, Texture2D> other)
            => ReferenceEquals(this, other);
    }
}