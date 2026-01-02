using System;
using System.Collections.Generic;
using UnityEngine;

using Rayforge.Infrastructure.Collections.Helpers;

namespace Rayforge.Infrastructure.Collections
{
    /// <summary>
    /// Represents an "unsafe" mip chain with additional flexibility for advanced scenarios.
    /// This class extends <see cref="MipChain{Thandle, Tdata}"/> and allows:
    /// - Creating mip levels starting at arbitrary indices in the handle array.
    /// - Optionally shrinking the handle array to exactly fit the created mip levels.
    /// - Stacking multiple mip chains into a single handle array.
    /// 
    /// Use with caution: manipulating start indices and shrink behavior can break
    /// the assumptions of a standard mip chain, so this class is intended for advanced usage
    /// where you explicitly want to bypass the safety guarantees of <see cref="MipChain{Thandle, Tdata}"/>.
    /// </summary>
    /// <typeparam name="Thandle">Type of the handle (e.g., TextureHandle, RenderTexture, etc.).</typeparam>
    /// <typeparam name="Tdata">Optional user data passed to the creation function for context or parameters.</typeparam>
    public class UnsafeMipChain<Thandle, Tdata> : MipChain<Thandle, Tdata>
    {
        /// <summary>
        /// Initializes the mip chain with a handle creation function.
        /// </summary>
        /// <param name="createFunc">Function to create each mip level.</param>
        public UnsafeMipChain(CreateFunction createFunc)
            : base(createFunc)
        { }

        /// <summary>
        /// Creates only the first mip level from the specified <see cref="DescriptorMipChain"/>.
        /// Handles are stored at index 0 in the handle array.
        /// The handle array is only enlarged if necessary; it will not be shrunk,
        /// so existing handles in the array are preserved.
        /// </summary>
        /// <param name="descriptorChain">The descriptor chain providing the descriptor for the first mip level.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void CreateFirst(DescriptorMipChain descriptorChain, Tdata data = default)
            => CreateUnsafe(descriptorChain, 0, 1, 0, false, data);

        /// <summary>
        /// Creates a range of mip levels from the specified <see cref="DescriptorMipChain"/>.
        /// Handles are stored at the same indices as their corresponding descriptors.
        /// The handle array is only enlarged if necessary; it will never be shrunk.
        /// </summary>
        /// <param name="descriptorChain">The descriptor chain providing descriptors for the mip levels.</param>
        /// <param name="startMip">Index of the first mip level to create.</param>
        /// <param name="count">Number of mip levels to create.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void CreateUnsafe(DescriptorMipChain descriptorChain, int startMip, int count, Tdata data = default)
            => CreateUnsafe(descriptorChain, startMip, count, startMip, false, data);

        /// <summary>
        /// Creates a range of mip levels from the specified <see cref="DescriptorMipChain"/>
        /// and stores the resulting handles at a specified start index in the handle array.
        /// This allows stacking multiple mip chains into a single handle array.
        /// Shrink behavior can be controlled.
        /// </summary>
        /// <param name="descriptorChain">The descriptor chain providing descriptors for the mip levels.</param>
        /// <param name="startMip">Index of the first mip level to create from the descriptor chain.</param>
        /// <param name="count">Number of mip levels to create.</param>
        /// <param name="handleStartIndex">The start index in the handle array where the first handle will be stored.</param>
        /// <param name="shrink">
        /// If true, allows the handle array to be resized down if it is larger than needed; 
        /// otherwise, the array is only enlarged.
        /// </param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void CreateUnsafe(DescriptorMipChain descriptorChain, int startMip, int count, int handleStartIndex, bool shrink = false, Tdata data = default)
        {
            if (descriptorChain == null || descriptorChain.MipCount == 0)
                throw new ArgumentException("DescriptorMipChain must not be null or empty.", nameof(descriptorChain));

            var descriptors = descriptorChain.Descriptors;

            startMip = Mathf.Clamp(startMip, 0, descriptors.Count - 1);
            count = Mathf.Clamp(count, 1, descriptors.Count - startMip);

            if (m_Handles.Length < handleStartIndex + count || shrink)
                Resize(handleStartIndex + count);

            for (int i = 0; i < count; i++)
                Create(handleStartIndex + i, descriptors[startMip + i], data);
        }

        /// <summary>
        /// Creates a range of mip levels starting from <paramref name="startMip"/>.
        /// Handles are stored at indices starting from <paramref name="startMip"/> in the handle array.
        /// The handle array is only enlarged if necessary; it will never be shrunk in this overload. 
        /// </summary>
        /// <param name="width">Width of the base mip level.</param>
        /// <param name="height">Height of the base mip level.</param>
        /// <param name="descriptor">Base descriptor for mip creation; will be resized for each mip level.</param>
        /// <param name="startMip">Index of the first mip level to create.</param>
        /// <param name="count">Number of mip levels to create starting from <paramref name="startMip"/>.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void CreateUnsafe(int width, int height, RenderTextureDescriptor descriptor, int startMip, int count, Tdata data = default)
            => CreateUnsafe(width, height, descriptor, startMip, count, startMip, false, data);

        /// <summary>
        /// Creates a range of mip levels with full control over the handle array.
        /// Handles are stored starting at <paramref name="handleStartIndex"/> in the handle array.
        /// If <paramref name="shrink"/> is true, the handle array may be reduced to exactly fit the created handles. 
        /// If false, the array will only be enlarged if necessary.
        /// This overload is useful for stacking multiple mip chains into a single handle array or for special handle layouts.
        /// </summary>
        /// <param name="width">Width of the base mip level.</param>
        /// <param name="height">Height of the base mip level.</param>
        /// <param name="descriptor">Base descriptor for mip creation; will be resized for each mip level.</param>
        /// <param name="startMip">Index of the first mip level to create.</param>
        /// <param name="count">Number of mip levels to create starting from <paramref name="startMip"/>.</param>
        /// <param name="handleStartIndex">Start index in the handle array where the first handle will be stored.</param>
        /// <param name="shrink">
        /// If true, allows the handle array to be resized down if it is larger than needed; otherwise only enlarges.
        /// </param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void CreateUnsafe(int width, int height, RenderTextureDescriptor descriptor, int startMip, int count, int handleStartIndex, bool shrink = false, Tdata data = default)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Base width and height must be greater than zero.");

            if (count <= 0)
                throw new ArgumentException("Count must be greater than zero.");

            Vector2Int baseRes = new Vector2Int(width, height);

            if (m_Handles.Length < handleStartIndex + count || shrink)
                Resize(handleStartIndex + count);

            for (int i = 0; i < count; i++)
            {
                var mipRes = MipChainHelpers.DefaultMipResolution(startMip + i, baseRes);
                descriptor.width = mipRes.x;
                descriptor.height = mipRes.y;

                Create(handleStartIndex + i, descriptor, data);
            }
        }

        /// <summary>
        /// Sets a handle at an arbitrary index in the mip chain.
        /// 
        /// This method bypasses all mip chain consistency guarantees:
        /// - The index does not need to correspond to a valid mip level.
        /// - The handle array may grow and contain gaps.
        /// - No validation of mip ordering or resolution is performed.
        /// 
        /// Intended for advanced scenarios where mip chain invariants
        /// are managed externally by the caller.
        /// </summary>
        /// <param name="index">Target index in the handle array.</param>
        /// <param name="handle">Handle to assign.</param>
        public void SetHandleUnsafe(int index, Thandle handle)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            var expectedSize = index + 1;
            if (m_Handles.Length < expectedSize)
                Resize(expectedSize);

            m_Handles[index] = handle;
        }

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
        /// <param name="handleStartIndex">Start index in the handle array where the first handle will be stored.</param>
        public void CopyFromUnsafe(MipChain<Thandle, Tdata> other, int start, int count, int handleStartIndex)
            => CopyFromUnsafe(other.Handles, start, count, handleStartIndex);

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
        /// <param name="handleStartIndex">Start index in the handle array where the first handle will be stored.</param>
        public void CopyFromUnsafe(IReadOnlyList<Thandle> other, int start, int count, int handleStartIndex)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (handleStartIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(handleStartIndex));

            if (other.Count == 0 || count <= 0)
                return;

            start = Math.Clamp(start, 0, other.Count);
            count = Math.Clamp(count, 0, other.Count - start);

            var requiredSize = handleStartIndex + count;
            if (m_Handles.Length < requiredSize)
                Resize(requiredSize);

            for (int i = 0; i < count; i++)
                m_Handles[handleStartIndex + i] = other[start + i];
        }
    }
}