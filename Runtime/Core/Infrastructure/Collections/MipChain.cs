using System;
using System.Collections.Generic;
using UnityEngine;

using Rayforge.Infrastructure.Abstractions;
using Rayforge.Infrastructure.Collections.Helpers;

namespace Rayforge.Infrastructure.Collections
{
    /// <summary>
    /// Represents a chain of handles corresponding to mip levels of a texture.
    /// Provides creation, resizing, copying, and optional generation of successive mip levels.
    /// </summary>
    /// <typeparam name="Thandle">Type of the handle (e.g., TextureHandle, RenderTexture, etc.).</typeparam>
    /// <typeparam name="Tdata">Optional user data passed to the creation function for context or parameters.</typeparam>
    public class MipChain<Thandle, Tdata> : IRenderingCollection<Thandle>
    {
        /// <summary>
        /// Delegate for creating a handle for a mip level.
        /// </summary>
        /// <param name="descriptor">Descriptor describing the texture to create.</param>
        /// <param name="mipLevel">Index of the mip level being created.</param>
        /// <param name="data">Optional user data.</param>
        /// <returns>True if creation was successful; otherwise false.</returns>
        public delegate Thandle CreateFunction(RenderTextureDescriptor descriptor, int mipLevel, Tdata data = default);

        /// <summary>
        /// Delegate for generating mip maps between two handles.
        /// </summary>
        /// <param name="src">Source handle (previous level).</param>
        /// <param name="dest">Destination handle (current level).</param>
        /// <param name="mipLevel">The mip level index being generated.</param>
        public delegate void GenerateFunction(Thandle src, Thandle dest, int mipLevel);

        protected Thandle[] m_Handles;
        private CreateFunction m_CreateFunc;

        /// <summary>Read-only access to the handles.</summary>
        public IReadOnlyList<Thandle> Handles => m_Handles ?? Array.Empty<Thandle>();

        /// <summary>Access a specific mip level handle by index.</summary>
        /// <param name="index">The mip level index.</param>
        public Thandle this[int index] => m_Handles[index];

        /// <summary>Total number of mip levels.</summary>
        public int MipCount => m_Handles?.Length ?? 0;

        /// <summary>
        /// Initializes the mip chain with a handle creation function.
        /// </summary>
        /// <param name="createFunc">Function to create each mip level.</param>
        public MipChain(CreateFunction createFunc)
        {
            if (createFunc == null)
                throw new ArgumentNullException(nameof(createFunc));

            m_CreateFunc = createFunc;
            m_Handles = Array.Empty<Thandle>();
        }

        /// <summary>
        /// Creates all mip levels from the specified <see cref="DescriptorMipChain"/>.
        /// Handles are stored at indices starting from 0 in the handle array.
        /// The handle array is resized to exactly match the number of mip levels in the chain.
        /// </summary>
        /// <param name="descriptorChain">The descriptor chain providing descriptors for each mip level.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void Create(DescriptorMipChain descriptorChain, Tdata data = default)
        {
            if (descriptorChain == null || descriptorChain.MipCount == 0)
                throw new ArgumentException("DescriptorMipChain must not be null or empty.", nameof(descriptorChain));

            var descriptors = descriptorChain.Descriptors;

            Resize(descriptors.Count);
            for (int i = 0; i < descriptors.Count; i++)
                Create(i, descriptors[i], data);
        }

        /// <summary>
        /// Creates all mip levels based on a single <see cref="RenderTextureDescriptor"/> as the base descriptor.
        /// Handles are stored at indices starting from 0 in the handle array.
        /// The handle array is resized to exactly match the number of mip levels being created. 
        /// If it was previously larger or smaller, it will be resized to <paramref name="mipCount"/>.
        /// </summary>
        /// <param name="width">Width of the base mip level.</param>
        /// <param name="height">Height of the base mip level.</param>
        /// <param name="descriptor">Base descriptor for mip creation; will be resized for each mip level.</param>
        /// <param name="mipCount">Total number of mip levels to create.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void Create(int width, int height, RenderTextureDescriptor descriptor, int mipCount = 1, Tdata data = default)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Base width and height must be greater than zero.");

            if (mipCount <= 0)
                throw new ArgumentException("Count must be greater than zero.");

            Vector2Int baseRes = new Vector2Int(width, height);

            Resize(mipCount);
            for (int i = 0; i < mipCount; i++)
            {
                var mipRes = MipChainHelpers.DefaultMipResolution(i, baseRes);
                descriptor.width = mipRes.x;
                descriptor.height = mipRes.y;

                Create(i, descriptor, data);
            }
        }

        /// <summary>
        /// Internal method that invokes the creation delegate for a single mip level.
        /// </summary>
        /// <param name="index">Index of the mip level to create.</param>
        /// <param name="descriptor">Descriptor to use for this mip level.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        protected void Create(int index, RenderTextureDescriptor descriptor, Tdata data = default)
            => m_Handles[index] = m_CreateFunc.Invoke(descriptor, index, data);

        /// <summary>
        /// Resizes the internal array to <paramref name="newLength"/>.
        /// </summary>
        /// <param name="newLength">New array length.</param>
        public void Resize(int newLength)
            => Resize(newLength, 0, MipCount);

        /// <summary>
        /// Resizes the array and optionally preserves a subset of existing elements.
        /// </summary>
        /// <param name="newLength">New array length.</param>
        /// <param name="preserveIndex">Start index in the old array to preserve.</param>
        /// <param name="preserveCount">Number of elements to preserve.</param>
        public void Resize(int newLength, int preserveIndex, int preserveCount)
        {
            if (newLength < 0) newLength = 0;
            if (MipCount == newLength) return;
            if (newLength == 0)
            {
                m_Handles = Array.Empty<Thandle>();
                return;
            }

            var newHandles = new Thandle[newLength];

            if (m_Handles != null && preserveCount > 0)
            {
                preserveIndex = Math.Clamp(preserveIndex, 0, m_Handles.Length - 1);
                preserveCount = Math.Min(preserveCount, m_Handles.Length - preserveIndex);
                preserveCount = Math.Min(preserveCount, newHandles.Length);

                Array.Copy(m_Handles, preserveIndex, newHandles, 0, preserveCount);
            }

            m_Handles = newHandles;
        }

        /// <summary>
        /// Returns a read-only span of handles.
        /// </summary>
        public ReadOnlySpan<Thandle> AsSpan()
            => m_Handles.AsSpan(0, MipCount);

        /// <summary>
        /// Returns a read-only span of handles.
        /// </summary>
        /// <param name="start">Start index of the span.</param>
        /// <param name="length">Number of elements in the span.</param>
        public ReadOnlySpan<Thandle> AsSpan(int start, int length)
        {
            start = Math.Clamp(start, 0, MipCount);
            length = Math.Clamp(length, 0, MipCount - start);
            return m_Handles.AsSpan(start, length);
        }

        /// <summary>
        /// Copies all handles from another mip chain.
        /// </summary>
        /// <param name="other">Source mip chain.</param>
        public void CopyFrom(MipChain<Thandle, Tdata> other)
            => CopyFrom(other, 0, other.MipCount);

        /// <summary>
        /// Copies a range of handles from another mip chain.
        /// <para>
        /// This method can bypass the usual safety guarantees of a mip chain
        /// (for example, contiguous layout or complete mip coverage) and is
        /// intended for advanced usage where such constraints are managed manually.
        /// </para>
        /// </summary>
        /// <param name="other">Source mip chain.</param>
        /// <param name="start">Start index in the source chain.</param>
        /// <param name="count">Number of handles to copy.</param>
        public void CopyFrom(MipChain<Thandle, Tdata> other, int start, int count)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            start = Math.Clamp(start, 0, other.MipCount);
            count = Math.Clamp(count, 0, other.MipCount - start);

            Resize(count);
            for (int i = 0; i < count; i++)
                m_Handles[i] = other[start + i];
        }

        /// <summary>
        /// Creates a MipChain from a single handle. The chain will have length 1.
        /// Useful when no actual mip levels are needed and a single texture/handle represents the entire chain.
        /// </summary>
        /// <param name="handle">The single handle representing the chain.</param>
        public void CopyFrom(Thandle handle)
        {
            Resize(1);
            m_Handles[0] = handle;
        }

        /// <summary>
        /// Generates mip maps for the chain using the provided delegate.
        /// </summary>
        /// <param name="generateFunc">Function that generates a mip from source to destination.</param>
        public void GenerateMipMaps(GenerateFunction generateFunc)
        {
            if (MipCount <= 1 || generateFunc == null) return;
            for (int i = 1; i < MipCount; i++)
            {
                generateFunc(m_Handles[i - 1], m_Handles[i], i);
            }
        }
    }
}