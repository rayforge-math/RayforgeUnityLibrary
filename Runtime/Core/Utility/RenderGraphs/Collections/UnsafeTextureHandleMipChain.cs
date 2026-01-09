using System;
using UnityEngine.Rendering.RenderGraphModule;
using Rayforge.Core.Common;
using Rayforge.Core.Rendering.Collections;

namespace Rayforge.Core.Utility.RenderGraphs.Collections
{
    /// <summary>
    /// Represents an "unsafe" variant of <see cref="TextureHandleMipChain{TData}"/>.
    /// 
    /// This class inherits from <see cref="UnsafeMipChain{THandle,TData}"/> and exposes 
    /// advanced functionality not available in the safe <see cref="TextureHandleMipChain{TData}"/>:
    /// - Checking ranges of mip handles for validity.
    /// - Copying subsets of chains or stacking multiple chains into one array.
    /// - Explicit control over handle array resizing and layout.
    ///
    /// Use this class only when you need these low-level capabilities and accept responsibility 
    /// for maintaining consistency. For most scenarios, prefer the safe 
    /// <see cref="TextureHandleMipChain{TData}"/> which provides the same basic functionality 
    /// without exposing unsafe operations.
    ///
    /// Redundant <see cref="IsValid()"/> methods are provided for API consistency with the safe variant.
    /// </summary>
    /// <typeparam name="TData">
    /// Optional user data passed to the texture creation function, useful for passing context
    /// or resources needed during RenderGraph allocation.
    /// </typeparam>
    public class UnsafeTextureHandleMipChain<TData> : UnsafeMipChain<TextureHandle, TData>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UnsafeTextureHandleMipChain{TData}"/> with a texture creation function.
        /// </summary>
        /// <param name="createFunc">Function invoked to create each mip level.</param>
        public UnsafeTextureHandleMipChain(CreateFunction createFunc)
            : base(createFunc)
        { }

        /// <summary>
        /// Checks whether all mip handles in the chain are valid.
        /// </summary>
        /// <returns><c>true</c> if all mip handles are valid; otherwise, <c>false</c>.</returns>
        public bool IsValid()
        {
            foreach (var handle in Handles)
            {
                if (!handle.IsValid())
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Checks whether the mip handle at the specified index is valid.
        /// </summary>
        /// <param name="mip">Zero-based index of the mip level to check.</param>
        /// <returns><c>true</c> if the mip handle is valid; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="mip"/> is out of range [0-<see cref="UnsafeMipChain{THandle,TData}.Handles"/>.Count).</exception>
        public bool IsValid(int mip)
        {
            if (mip < 0 || mip >= Handles.Count)
                throw new ArgumentOutOfRangeException(nameof(mip), $"Mip index must be between 0 and {Handles.Count - 1}.");
            return Handles[mip].IsValid();
        }

        /// <summary>
        /// Checks whether all mip handles in the specified range are valid.
        /// </summary>
        /// <param name="startMip">Zero-based index of the first mip level to check.</param>
        /// <param name="count">Number of consecutive mip levels to check starting from <paramref name="startMip"/>.</param>
        /// <returns><c>true</c> if all mip handles in the range are valid; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified range is out of bounds.</exception>
        public bool IsValid(int startMip, int count)
        {
            if (startMip < 0 || startMip >= Handles.Count)
                throw new ArgumentOutOfRangeException(nameof(startMip), $"Start mip index must be between 0 and {Handles.Count - 1}.");
            if (count <= 0 || startMip + count > Handles.Count)
                throw new ArgumentOutOfRangeException(nameof(count), $"Count must be positive and within the range of available handles.");

            for (int i = startMip; i < startMip + count; i++)
            {
                if (!IsValid(i))
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Represents an "unsafe" variant of <see cref="TextureHandleMipChain{TData}"/>.
    /// 
    /// This class inherits from <see cref="UnsafeMipChain{THandle,TData}"/> and exposes 
    /// advanced functionality not available in the safe <see cref="TextureHandleMipChain{TData}"/>:
    /// - Checking ranges of mip handles for validity.
    /// - Copying subsets of chains or stacking multiple chains into one array.
    /// - Explicit control over handle array resizing and layout.
    ///
    /// Use this class only when you need these low-level capabilities and accept responsibility 
    /// for maintaining consistency. For most scenarios, prefer the safe 
    /// <see cref="TextureHandleMipChain{TData}"/> which provides the same basic functionality 
    /// without exposing unsafe operations.
    ///
    /// Redundant <see cref="IsValid()"/> methods are provided for API consistency with the safe variant.
    /// </summary>
    public class UnsafeTextureHandleMipChain : UnsafeTextureHandleMipChain<NoData>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UnsafeTextureHandleMipChain{TData}"/> with a texture creation function.
        /// </summary>
        /// <param name="createFunc">Function invoked to create each mip level.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="createFunc"/> is <c>null</c>.
        /// </exception>
        public UnsafeTextureHandleMipChain(CreateFunction createFunc)
            : base(createFunc)
        { }
    }
}