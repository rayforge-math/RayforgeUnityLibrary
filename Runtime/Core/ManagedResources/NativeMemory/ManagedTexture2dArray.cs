using Rayforge.Core.ManagedResources.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Managed wrapper around Unity's <see cref="Texture2DArray"/>.
    /// Provides creation, validation, and controlled release.
    /// </summary>
    public sealed class ManagedTexture2DArray : ManagedBuffer<Texture2dArrayDescriptor, Texture2DArray>
    {
        /// <summary>
        /// Private constructor used internally to wrap an existing <see cref="Texture2DArray"/> and descriptor.
        /// </summary>
        private ManagedTexture2DArray(Texture2DArray texture, Texture2dArrayDescriptor descriptor)
            : base(texture, descriptor)
        { }

        /// <summary>
        /// Creates a managed Texture2DArray from the provided descriptor.
        /// Validates dimensions and layer count before allocation.
        /// </summary>
        /// <param name="desc">Descriptor defining each texture in the array and number of layers.</param>
        /// <returns>A new <see cref="ManagedTexture2DArray"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <see cref="Texture2dArrayDescriptor.Count"/>, Width or Height is less than 1.
        /// </exception>
        public static ManagedTexture2DArray Create(Texture2dArrayDescriptor desc)
        {
            var d = desc.Descriptor;

            if (desc.Count <= 0)
                throw new ArgumentOutOfRangeException(nameof(desc.Count), "Texture2DArray count must be > 0.");
            if (d.Width <= 0)
                throw new ArgumentOutOfRangeException(nameof(d.Width), "Texture width must be > 0.");
            if (d.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(d.Height), "Texture height must be > 0.");

            var texture = new Texture2DArray(
                d.Width,
                d.Height,
                desc.Count,
                d.ColorFormat,
                d.MipCount > 1,
                d.Linear)
            {
                filterMode = d.FilterMode,
                wrapMode = d.WrapMode,
                anisoLevel = 0
            };
            texture.Apply(false);

            return new ManagedTexture2DArray(texture, desc);
        }

        /// <summary>
        /// Releases the underlying Texture2DArray.
        /// After calling this, the buffer is no longer valid.
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

            var descriptor = m_Descriptor.Descriptor;
            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i] == null)
                {
                    Debug.LogError($"Texture at index {i} is null");
                    return false;
                }

                if (textures[i].width != descriptor.Width || textures[i].height != descriptor.Height)
                {
                    Debug.LogError($"Texture at index {i} has mismatched dimensions. " +
                                $"Expected: {descriptor.Width}x{descriptor.Height}, Got: {textures[i].width}x{textures[i].height}");
                    return false;
                }

                if (textures[i].format != descriptor.ColorFormat)
                {
                    Debug.LogWarning($"Texture at index {i} has format {textures[i].format}, " +
                                    $"but expected {descriptor.ColorFormat}. This may cause conversion overhead.");
                }

                if (textures[i].mipmapCount != descriptor.MipCount)
                {
                    Debug.LogWarning($"Texture at index {i} has mipmap count {textures[i].mipmapCount}, " +
                                    $"but expected {descriptor.MipCount}. This may result in faulty graphics.");
                }
            }

            if (textures.Length > m_Descriptor.Count)
            {
                Debug.LogWarning($"More textures ({textures.Length}) provided than array size ({m_Descriptor.Count}). " +
                                $"Only the first {m_Descriptor.Count} will be used.");
            }

            try
            {
                int texturesToCopy = Mathf.Min(textures.Length, m_Descriptor.Count);
                for (int i = 0; i < texturesToCopy; i++)
                {
                    for (int j = 0; j < descriptor.MipCount; ++j)
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