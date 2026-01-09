using System;
using System.Collections.Generic;
using UnityEngine;

using Rayforge.Core.Rendering.Collections.Helpers;

namespace Rayforge.Core.Rendering.Collections
{
    /// <summary>
    /// Represents an "unsafe" mip chain with additional flexibility for advanced scenarios.
    /// This class extends <see cref="MipChain{THandle, TData}"/> and allows:
    /// - Creating mip levels starting at arbitrary indices in the handle array.
    /// - Optionally shrinking the handle array to exactly fit the created mip levels.
    /// - Stacking multiple mip chains into a single handle array.
    /// 
    /// Use with caution: manipulating start indices and shrink behavior can break
    /// the assumptions of a standard mip chain, so this class is intended for advanced usage
    /// where you explicitly want to bypass the safety guarantees of <see cref="MipChain{THandle, TData}"/>.
    /// </summary>
    /// <typeparam name="THandle">Type of the handle (e.g., TextureHandle, RenderTexture, etc.).</typeparam>
    /// <typeparam name="TData">Optional user data passed to the creation function for context or parameters.</typeparam>
    public class UnsafeMipChain<THandle, TData> : MipChain<THandle, TData>
    {
        private Vector2Int[] m_MipResolutionCache;
        private static readonly Vector2Int k_EmptyCacheEntry = Vector2Int.zero;

        /// <summary>
        /// Initializes the mip chain with a handle creation function.
        /// </summary>
        /// <param name="createFunc">Function to create each mip level.</param>
        public UnsafeMipChain(CreateFunction createFunc)
            : base(createFunc)
        {
            m_MipResolutionCache = Array.Empty<Vector2Int>();

            m_CreateFunc = (ref THandle handle, RenderTextureDescriptor desc, int mipLevel, TData data) =>
            {
                bool created = createFunc(ref handle, desc, mipLevel, data);

                if (created)
                {
                    if (m_MipResolutionCache.Length <= mipLevel)
                        Array.Resize(ref m_MipResolutionCache, mipLevel + 1);

                    m_MipResolutionCache[mipLevel] = new Vector2Int(desc.width, desc.height);
                }

                return created;
            };
        }

        /// <summary>
        /// Retrieves the resolution of the specified mip level, using the cached value if available.
        /// </summary>
        /// <param name="mipLevel">
        /// Index of the mip level to query (0 = base level, 1 = first mip, etc.).
        /// </param>
        /// <returns>
        /// The <see cref="Vector2Int"/> representing the width and height of the mip level.
        /// <para/>
        /// - If the mip level was previously created in this <see cref="UnsafeMipChain"/>, 
        ///   the cached real resolution is returned.
        /// - Otherwise, the theoretical default resolution from the base <see cref="MipChain"/> is returned.
        /// </returns>
        /// <remarks>
        /// This method ensures that the resolution always reflects the last created state for each mip level.
        /// Caching all created mip resolutions avoids repeatedly recalculating or reading them from the descriptor,
        /// and guarantees consistency for any runtime queries.
        /// </remarks>
        public Vector2Int GetCachedMipResolution(int mipLevel)
        {
            if (mipLevel < m_MipResolutionCache.Length)
                return m_MipResolutionCache[mipLevel];

            return GetDefaultMipResolution(mipLevel);
        }

        /// <summary>
        /// Resets or resizes the mip resolution cache starting at a specific index.
        /// </summary>
        /// <param name="startIndex">
        /// The mip level index at which to start resetting or truncating the cache.
        /// Must be between 0 and the current cache length.
        /// </param>
        /// <param name="count">
        /// The number of entries to reset.  
        /// <list type="bullet">
        /// <item>If <c>0</c> or greater than the remaining elements, the cache is truncated at <paramref name="startIndex"/>.</item>
        /// <item>Otherwise, only the specified range is reset to <c>k_EmptyCacheEntry</c>.</item>
        /// </list>
        /// </param>
        /// <remarks>
        /// - Truncating the cache does not preserve any entries beyond <paramref name="startIndex"/>.
        /// - Resetting individual entries sets them to <c>k_EmptyCacheEntry</c>, typically <c>Vector2Int.zero</c>,
        ///   which causes <see cref="GetCachedMipResolution"/> to fall back to the default resolution.
        /// </remarks>
        public void ResetResolutionCache(int startIndex, int count = 0)
        {
            if (startIndex < 0 || startIndex > m_MipResolutionCache.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            int remaining = m_MipResolutionCache.Length - startIndex;

            // Truncate cache if count is 0 or exceeds remaining entries
            if (count == 0 || count >= remaining)
            {
                if (startIndex == 0)
                {
                    // Reset entire cache
                    m_MipResolutionCache = Array.Empty<Vector2Int>();
                }
                else
                {
                    // Truncate cache at startIndex
                    Array.Resize(ref m_MipResolutionCache, startIndex);
                }
            }
            else
            {
                // Reset only the specified range to empty
                for (int i = startIndex; i < startIndex + count; i++)
                    m_MipResolutionCache[i] = k_EmptyCacheEntry;
            }
        }

        /// <summary>
        /// Completely resets the mip resolution cache.
        /// </summary>
        public void ResetResolutionCache()
            => ResetResolutionCache(0, 0);

        /// <summary>
        /// Creates only the first mip level from the specified <see cref="DescriptorMipChain"/>.
        /// Handles are stored at index 0 in the handle array.
        /// The handle array is only enlarged if necessary; it will not be shrunk,
        /// so existing handles in the array are preserved.
        /// </summary>
        /// <param name="descriptorChain">The descriptor chain providing the descriptor for the first mip level.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void CreateFirst(DescriptorMipChain descriptorChain, TData data = default)
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
        public void CreateUnsafe(DescriptorMipChain descriptorChain, int startMip, int count, TData data = default)
            => CreateUnsafe(descriptorChain, startMip, count, startMip, false, data);

        /// <summary>
        /// Creates a range of mip levels from the specified <see cref="DescriptorMipChain"/>.
        /// Handles are stored at the same indices as their corresponding descriptors.
        /// The handle array is only enlarged if necessary; it will never be shrunk.
        /// </summary>
        /// <param name="descriptorChain">The descriptor chain providing descriptors for the mip levels.</param>
        /// <param name="startMip">Index of the first mip level to create.</param>
        /// <param name="count">Number of mip levels to create.</param>
        /// <param name="shrink">
        /// If true, allows the handle array to be resized down if it is larger than needed; 
        /// otherwise, the array is only enlarged.
        /// </param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void CreateUnsafe(DescriptorMipChain descriptorChain, int startMip, int count, bool shrink = false, TData data = default)
            => CreateUnsafe(descriptorChain, startMip, count, startMip, shrink, data);

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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="descriptorChain"/> is null.</exception>
        public void CreateUnsafe(DescriptorMipChain descriptorChain, int startMip, int count, int handleStartIndex, bool shrink = false, TData data = default)
        {
            if (descriptorChain == null)
                throw new ArgumentException("DescriptorMipChain must not be null or empty.", nameof(descriptorChain));

            var descriptors = descriptorChain.Descriptors;
            var descCount = descriptors == null ? 0 : descriptors.Count;

            startMip = Mathf.Clamp(startMip, 0, descCount - 1);
            count = Mathf.Clamp(count, 1, descCount - startMip);

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
        /// <param name="descriptor">Base descriptor for mip creation; will be resized for each mip level.</param>
        /// <param name="startMip">Index of the first mip level to create.</param>
        /// <param name="count">Number of mip levels to create starting from <paramref name="startMip"/>.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        /// <exception cref="ArgumentException">Thrown if the descriptor width or height is not positive.</exception>
        public void CreateUnsafe(RenderTextureDescriptor descriptor, int startMip, int count, TData data = default)
            => CreateUnsafe(descriptor.width, descriptor.height, descriptor, startMip, count, startMip, false, data);

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
        /// <exception cref="ArgumentException">Thrown if the descriptor width or height is not positive.</exception>
        public void CreateUnsafe(int width, int height, RenderTextureDescriptor descriptor, int startMip, int count, TData data = default)
            => CreateUnsafe(width, height, descriptor, startMip, count, startMip, false, data);

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
        /// <param name="shrink">
        /// If true, allows the handle array to be resized down if it is larger than needed; 
        /// otherwise, the array is only enlarged.
        /// </param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        /// <exception cref="ArgumentException">Thrown if the descriptor width or height is not positive.</exception>
        public void CreateUnsafe(int width, int height, RenderTextureDescriptor descriptor, int startMip, int count, bool shrink = false, TData data = default)
            => CreateUnsafe(width, height, descriptor, startMip, count, startMip, shrink, data);

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
        /// <exception cref="ArgumentException">Thrown if the descriptor width or height is not positive.</exception>
        public void CreateUnsafe(int width, int height, RenderTextureDescriptor descriptor, int startMip, int count, int handleStartIndex, bool shrink = false, TData data = default)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Base width and height must be greater than zero.");

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
        /// <exception cref="ArgumentOutOfRangeException">Thrown if index is not positive or 0.</exception>
        public void SetHandleUnsafe(int index, THandle handle)
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="other"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="handleStartIndex"/> is negative.</exception>
        public void CopyFromUnsafe(MipChain<THandle, TData> other, int start, int count, int handleStartIndex)
            => CopyFromUnsafe(other.Handles, start, count, handleStartIndex);

        /// <summary>
        /// Copies a range of handles from another collection of handles.
        /// <para>
        /// This method can bypass the usual safety guarantees of a mip chain
        /// (for example, contiguous layout or complete mip coverage) and is
        /// intended for advanced usage where such constraints are managed manually.
        /// </para>
        /// </summary>
        /// <param name="other">Source collection of handles.</param>
        /// <param name="start">Start index in the source collection.</param>
        /// <param name="count">Number of handles to copy.</param>
        /// <param name="handleStartIndex">Start index in the handle array where the first handle will be stored.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="other"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="handleStartIndex"/> is negative.</exception>
        public void CopyFromUnsafe(IReadOnlyList<THandle> other, int start, int count, int handleStartIndex)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (handleStartIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(handleStartIndex), "Start index must be non-negative.");

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