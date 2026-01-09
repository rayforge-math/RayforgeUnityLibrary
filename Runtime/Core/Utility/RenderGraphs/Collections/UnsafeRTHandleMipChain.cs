using Rayforge.Core.Common;
using Rayforge.Core.Rendering.Collections;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.Core.Utility.RenderGraphs.Collections
{
    /// <summary>
    /// Represents an "unsafe" variant of <see cref="RTHandleMipChain{TData}"/>.
    /// 
    /// This class inherits from <see cref="UnsafeMipChain{THandle,TData}"/> and exposes 
    /// advanced functionality not available in the safe <see cref="RTHandleMipChain{TData}"/>:
    /// - Checking ranges of mip handles for validity.
    /// - Copying subsets of chains or stacking multiple chains into one array.
    /// - Explicit control over handle array resizing and layout.
    ///
    /// Use this class only when you need these low-level capabilities and accept responsibility 
    /// for maintaining consistency. For most scenarios, prefer the safe 
    /// <see cref="RTHandleMipChain{TData}"/> which provides the same basic functionality 
    /// without exposing unsafe operations.
    ///
    /// Redundant `IsValid` methods are provided for API consistency with the safe variant.
    /// </summary>
    /// <typeparam name="TData">
    /// Optional user data passed to the texture creation function, useful for passing context
    /// or resources needed during RenderGraph allocation.
    /// </typeparam>
    public class UnsafeRTHandleMipChain<TData> : UnsafeMipChain<RTHandle, TData>
    {
        /// <summary>
        /// Initializes a mip chain with a texture creation function.
        /// </summary>
        /// <param name="createFunc">Function to create each mip level.</param>
        public UnsafeRTHandleMipChain(CreateFunction createFunc)
            : base(createFunc)
        { }
    }

    /// <summary>
    /// Represents an "unsafe" variant of <see cref="RTHandleMipChain{TData}"/>.
    /// 
    /// This class inherits from <see cref="UnsafeMipChain{THandle,TData}"/> and exposes 
    /// advanced functionality not available in the safe <see cref="RTHandleMipChain{TData}"/>:
    /// - Checking ranges of mip handles for validity.
    /// - Copying subsets of chains or stacking multiple chains into one array.
    /// - Explicit control over handle array resizing and layout.
    ///
    /// Use this class only when you need these low-level capabilities and accept responsibility 
    /// for maintaining consistency. For most scenarios, prefer the safe 
    /// <see cref="RTHandleMipChain{TData}"/> which provides the same basic functionality 
    /// without exposing unsafe operations.
    ///
    /// Redundant `IsValid` methods are provided for API consistency with the safe variant.
    /// </summary>
    /// <typeparam name="TData">
    /// Optional user data passed to the texture creation function, useful for passing context
    /// or resources needed during RenderGraph allocation.
    /// </typeparam>
    public class UnsafeRTHandleMipChain : UnsafeRTHandleMipChain<NoData>
    {
        /// <summary>
        /// Delegate for creating a handle for a mip level.
        /// </summary>
        /// <param name="handle">Reference to the current handle stored internally.</param>
        /// <param name="descriptor">Descriptor describing the texture to create.</param>
        /// <param name="mipLevel">Zero-based index of the mip level being created.</param>
        /// <returns>
        /// <c>true</c> if a new handle was created or allocated; 
        /// <c>false</c> if the existing handle was reused (e.g., when using <c>ReAllocateHandleIfNeeded</c>).
        /// </returns>
        public delegate bool CreateFunctionNoData(ref RTHandle handle, RenderTextureDescriptor descriptor, int mipLevel);

        /// <summary>
        /// Initializes a mip chain with a texture creation function.
        /// </summary>
        /// <param name="createFunc">Function to create each mip level.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="createFunc"/> is <c>null</c>.
        /// </exception>
        public UnsafeRTHandleMipChain(CreateFunctionNoData createFunc)
            : base((ref RTHandle handle, RenderTextureDescriptor descriptor, int mipLevel, NoData _) =>
            {
                return createFunc.Invoke(ref handle, descriptor, mipLevel);
            })
        {
            if (createFunc == null)
                throw new ArgumentNullException(nameof(createFunc), "The texture creation function must not be null.");
        }
    }
}