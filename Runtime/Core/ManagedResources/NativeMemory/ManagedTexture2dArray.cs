using Rayforge.ManagedResources.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.ManagedResources.NativeMemory
{
    /// <summary>
    /// Managed wrapper around Unity's <see cref="Texture2DArray"/>.
    /// Provides creation, validation, and controlled release.
    /// </summary>
    public sealed class ManagedTexture2DArray : ManagedBuffer<Texture2dArrayDescriptor, Texture2DArray>
    {
        public ManagedTexture2DArray(Texture2dArrayDescriptor descriptor)
            : base(CreateAndConfigure(descriptor), descriptor)
        { }

        /// <summary>
        /// Releases the underlying texture array.
        /// </summary>
        public override void Release()
        {
            if (m_Buffer != null)
            {
                Texture2DArray.Destroy(m_Buffer);
                m_Buffer = null;
            }
        }

        /// <summary>
        /// Instantiates and configures the Texture2DArray based on the descriptor.
        /// </summary>
        private static Texture2DArray CreateAndConfigure(Texture2dArrayDescriptor desc)
        {
            if (desc.count <= 0)
                throw new ArgumentException("Texture2DArray count must be > 0");
            if (desc.descriptor.width <= 0 || desc.descriptor.height <= 0)
                throw new ArgumentException("Texture dimensions must be > 0");

            var texture = new Texture2DArray(
                desc.descriptor.width,
                desc.descriptor.height,
                desc.count,
                desc.descriptor.colorFormat,
                desc.descriptor.mipCount > 1,
                desc.descriptor.linear
            );

            texture.filterMode = desc.descriptor.filterMode;
            texture.wrapMode = desc.descriptor.wrapMode;
            texture.anisoLevel = 0;
            texture.Apply(false);

            return texture;
        }

        /// <summary>
        /// Copies the provided textures into the array.
        /// Validates dimensions, format, and mip count before copying.
        /// </summary>
        public bool SetTextures(Texture2D[] textures)
        {
            if (textures == null || textures.Length == 0)
            {
                Debug.LogError("Texture array is null or empty");
                return false;
            }

            var descriptor = m_Descriptor.descriptor;
            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i] == null)
                {
                    Debug.LogError($"Texture at index {i} is null");
                    return false;
                }

                if (textures[i].width != descriptor.width || textures[i].height != descriptor.height)
                {
                    Debug.LogError($"Texture at index {i} has mismatched dimensions. " +
                                $"Expected: {descriptor.width}x{descriptor.height}, Got: {textures[i].width}x{textures[i].height}");
                    return false;
                }

                if (textures[i].format != descriptor.colorFormat)
                {
                    Debug.LogWarning($"Texture at index {i} has format {textures[i].format}, " +
                                    $"but expected {descriptor.colorFormat}. This may cause conversion overhead.");
                }

                if (textures[i].mipmapCount != descriptor.mipCount)
                {
                    Debug.LogWarning($"Texture at index {i} has mipmap count {textures[i].mipmapCount}, " +
                                    $"but expected {descriptor.mipCount}. This may result in faulty graphics.");
                }
            }

            if (textures.Length > m_Descriptor.count)
            {
                Debug.LogWarning($"More textures ({textures.Length}) provided than array size ({m_Descriptor.count}). " +
                                $"Only the first {m_Descriptor.count} will be used.");
            }

            try
            {
                int texturesToCopy = Mathf.Min(textures.Length, m_Descriptor.count);
                for (int i = 0; i < texturesToCopy; i++)
                {
                    for (int j = 0; j < descriptor.mipCount; ++j)
                    {
                        try
                        {
                            Graphics.CopyTexture(textures[i], 0, j, m_Buffer, i, j);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Copy texture {i} MipMap {j}: {ex}");
                        }
                    }
                }

                m_Buffer.Apply(false);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to copy textures: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Compares managed texture arrays by reference.
        /// Suitable for pooling or tracking.
        /// </summary>
        public override bool Equals(ManagedBuffer<Texture2dArrayDescriptor, Texture2DArray> other)
            => ReferenceEquals(this, other);
    }
}