using Rayforge.Core.Rendering.Abstractions;
using Rayforge.Core.Rendering.Collections.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Rendering.Collections
{
    /// <summary>
    /// Represents a chain of handles corresponding to mip levels of a texture.
    /// Provides creation, resizing, copying, and optional generation of successive mip levels.
    /// </summary>
    /// <typeparam name="THandle">Type of the handle (e.g., TextureHandle, RenderTexture, etc.).</typeparam>
    /// <typeparam name="TData">Optional user data passed to the creation function for context or parameters.</typeparam>
    public class MipChain<THandle, TData> : IRenderingCollection<THandle>
    {
        /// <summary>
        /// Delegate for creating a handle for a mip level.
        /// </summary>
        /// <param name="handle">Reference to the current handle stored internally.</param>
        /// <param name="descriptor">Descriptor describing the texture to create.</param>
        /// <param name="mipLevel">Index of the mip level being created.</param>
        /// <param name="data">Optional user data.</param>
        /// <returns>
        /// <c>true</c> if a new handle was created or allocated; 
        /// <c>false</c> if the existing handle was reused (e.g., when using <c>ReAllocateHandleIfNeeded</c>).
        /// </returns>
        public delegate bool CreateFunction(ref THandle handle, RenderTextureDescriptor descriptor, int mipLevel, TData data = default);

        /// <summary>
        /// Delegate for generating mip maps between two handles.
        /// </summary>
        /// <param name="src">Source handle (previous level).</param>
        /// <param name="dest">Destination handle (current level).</param>
        /// <param name="mipLevel">The mip level index being generated.</param>
        public delegate void GenerateFunction(THandle src, THandle dest, int mipLevel);

        protected THandle[] m_Handles;
        protected CreateFunction m_CreateFunc;

        private Vector2Int m_BaseResolution = new Vector2Int(-1 , -1);
        private static readonly Func<int, Vector2Int, Vector2Int> m_CalculateMipResFunc = MipChainHelpers.DefaultMipResolution;

        /// <summary>Read-only access to the handles.</summary>
        public IReadOnlyList<THandle> Handles => m_Handles ?? Array.Empty<THandle>();

        /// <summary>Access a specific mip level handle by index.</summary>
        /// <param name="index">The mip level index.</param>
        public THandle this[int index] => m_Handles[index];

        /// <summary>Total number of mip levels.</summary>
        public int MipCount => m_Handles?.Length ?? 0;

        /// <summary>
        /// Initializes the mip chain with a handle creation function.
        /// </summary>
        /// <param name="createFunc">Function to create each mip level.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="createFunc"/> is <c>null</c>.
        /// </exception>
        public MipChain(CreateFunction createFunc)
        {
            if (createFunc == null)
                throw new ArgumentNullException(nameof(createFunc));

            m_CreateFunc = createFunc;
            m_Handles = Array.Empty<THandle>();
        }

        /// <summary>
        /// Computes the theoretical resolution of the specified mip level, based on the base resolution.
        /// </summary>
        /// <param name="mipLevel">
        /// Index of the mip level to compute (0 = base level, 1 = first mip, etc.).
        /// </param>
        /// <returns>
        /// A <see cref="Vector2Int"/> representing the width and height of the mip level
        /// as defined by the configured mip resolution calculation function (default / theoretical).
        /// </returns>
        public Vector2Int GetDefaultMipResolution(int mipLevel)
            => m_CalculateMipResFunc(mipLevel, m_BaseResolution);

        /// <summary>
        /// Creates all mip levels from the specified <see cref="DescriptorMipChain"/>.
        /// Handles are stored at indices starting from 0 in the handle array.
        /// The handle array is resized to exactly match the number of mip levels in the chain.
        /// </summary>
        /// <param name="descriptorChain">The descriptor chain providing descriptors for each mip level.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        /// <returns><c>true</c> if at least one new handle was created; <c>false</c> if all handles were reused.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="descriptorChain"/> is null.</exception>
        public bool Create(DescriptorMipChain descriptorChain, TData data = default)
        {
            if (descriptorChain == null)
                throw new ArgumentNullException("DescriptorMipChain must not be null.", nameof(descriptorChain));

            var descriptors = descriptorChain.Descriptors;
            var count = descriptors == null ? 0 : descriptors.Count;
            Resize(count);

            bool anyCreated = false;
            for (int i = 0; i < count; i++)
                anyCreated |= Create(i, descriptors[i], data);

            return anyCreated;
        }

        /// <summary>
        /// Creates all mip levels based on a single <see cref="RenderTextureDescriptor"/> as the base descriptor.
        /// Handles are stored at indices starting from 0 in the handle array.
        /// The handle array is resized to exactly match the number of mip levels being created. 
        /// If it was previously larger or smaller, it will be resized to <paramref name="mipCount"/>.
        /// </summary>
        /// <param name="descriptor">Base descriptor for mip creation; will be resized for each mip level.</param>
        /// <param name="mipCount">Total number of mip levels to create.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        /// <returns><c>true</c> if at least one new handle was created; <c>false</c> if all handles were reused.</returns>
        /// <exception cref="ArgumentException">Thrown if the descriptor width or height is not positive.</exception>
        public bool Create(RenderTextureDescriptor descriptor, int mipCount = 1, TData data = default)
            => Create(descriptor.width, descriptor.height, descriptor, mipCount, data);

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
        /// <returns><c>true</c> if at least one new handle was created; <c>false</c> if all handles were reused.</returns>
        /// <exception cref="ArgumentException">Thrown if the descriptor width or height is not positive.</exception>
        public bool Create(int width, int height, RenderTextureDescriptor descriptor, int mipCount = 1, TData data = default)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Base width and height must be greater than zero.");

            m_BaseResolution = new Vector2Int(width, height);
            Resize(mipCount);

            bool anyCreated = false;
            for (int i = 0; i < mipCount; i++)
            {
                var mipRes = GetDefaultMipResolution(i);
                descriptor.width = mipRes.x;
                descriptor.height = mipRes.y;
                anyCreated |= Create(i, descriptor, data);
            }

            return anyCreated;
        }

        /// <summary>
        /// Internal method that invokes the creation delegate for a single mip level.
        /// </summary>
        /// <param name="index">Zero-based index of the mip level to create.</param>
        /// <param name="descriptor">Descriptor to use for this mip level.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        /// <returns>The result returned by the creation delegate (typically <c>true</c> if a new handle was created, <c>false</c> if reused).</returns>
        protected bool Create(int index, RenderTextureDescriptor descriptor, TData data = default)
            => m_CreateFunc.Invoke(ref m_Handles[index], descriptor, index, data);

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
                m_Handles = Array.Empty<THandle>();
                return;
            }

            var newHandles = new THandle[newLength];

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
        public ReadOnlySpan<THandle> AsSpan()
            => m_Handles == null
            ? ReadOnlySpan<THandle>.Empty
            : m_Handles.AsSpan(0, MipCount);

        /// <summary>
        /// Returns a read-only span of handles.
        /// </summary>
        /// <param name="start">Start index of the span.</param>
        /// <param name="length">Number of elements in the span.</param>
        public ReadOnlySpan<THandle> AsSpan(int start, int length)
        {
            if (m_Handles == null)
                return ReadOnlySpan<THandle>.Empty;

            start = Math.Clamp(start, 0, MipCount);
            length = Math.Clamp(length, 0, MipCount - start);
            return m_Handles.AsSpan(start, length);
        }

        /// <summary>
        /// Copies all handles from another mip chain.
        /// </summary>
        /// <param name="other">Source mip chain.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="other"/> is null.</exception>
        public void CopyFrom(MipChain<THandle, TData> other)
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="other"/> is null.</exception>
        public void CopyFrom(MipChain<THandle, TData> other, int start, int count)
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
        public void CopyFrom(THandle handle)
        {
            Resize(1);
            m_Handles[0] = handle;
        }

        /// <summary>
        /// Generates mip maps for the chain using the provided delegate.
        /// </summary>
        /// <param name="generateFunc">Function that generates a mip from source to destination.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="generateFunc"/> is null.</exception>
        public void GenerateMipMaps(GenerateFunction generateFunc)
        {
            if (generateFunc == null)
                throw new ArgumentNullException(nameof(generateFunc));

            if (MipCount <= 1) return;
            for (int i = 1; i < MipCount; i++)
            {
                generateFunc(m_Handles[i - 1], m_Handles[i], i);
            }
        }
    }
}