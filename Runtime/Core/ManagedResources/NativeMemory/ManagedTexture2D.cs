using Rayforge.ManagedResources.Abstractions;

using UnityEngine;

namespace Rayforge.ManagedResources.NativeMemory
{
    /// <summary>
    /// Managed wrapper around Unity's <see cref="Texture2D"/>.
    /// Provides creation, configuration, and controlled release for pooling or resource tracking.
    /// Inherits from <see cref="ManagedBuffer{TBuffer,TDesc}"/>.
    /// </summary>
    public sealed class ManagedTexture2D : ManagedBuffer<Texture2dDescriptor, Texture2D>
    {
        /// <summary>Width of the texture.</summary>
        public int Width => m_Descriptor.width;

        /// <summary>Height of the texture.</summary>
        public int Height => m_Descriptor.height;

        /// <summary>
        /// Creates a managed Texture2D with the provided descriptor.
        /// </summary>
        public ManagedTexture2D(Texture2dDescriptor desc)
            : base(CreateAndConfigureTexture(desc), desc)
        { }

        /// <summary>
        /// Instantiates the actual Texture2D object based on the descriptor.
        /// Configures filter and wrap modes.
        /// </summary>
        private static Texture2D CreateAndConfigureTexture(Texture2dDescriptor desc)
        {
            return new Texture2D(desc.width, desc.height, desc.colorFormat, desc.mipCount, desc.linear)
            {
                filterMode = desc.filterMode,
                wrapMode = desc.wrapMode
            };
        }

        /// <summary>
        /// Releases the underlying texture. After this call, the texture is no longer valid.
        /// Note: does not destroy the wrapper itself, enabling pooling or reuse.
        /// </summary>
        public override void Release()
            => m_Buffer = null;

        /// <summary>
        /// Compares managed textures by reference. Suitable for pooling or tracking.
        /// </summary>
        public override bool Equals(ManagedBuffer<Texture2dDescriptor, Texture2D> other)
            => ReferenceEquals(this, other);
    }
}