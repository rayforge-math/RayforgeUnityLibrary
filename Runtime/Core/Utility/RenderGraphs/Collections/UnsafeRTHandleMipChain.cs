using Rayforge.Common;
using Rayforge.Rendering.Collections;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.Utility.RenderGraphs.Collections
{
    /// <summary>
    /// Represents an "unsafe" variant of <see cref="RTHandleMipChain{Tdata}"/>.
    /// 
    /// This class inherits from <see cref="UnsafeMipChain{Thandle,Tdata}"/> and exposes 
    /// advanced functionality not available in the safe <see cref="RTHandleMipChain{Tdata}"/>:
    /// - Checking ranges of mip handles for validity.
    /// - Copying subsets of chains or stacking multiple chains into one array.
    /// - Explicit control over handle array resizing and layout.
    ///
    /// Use this class only when you need these low-level capabilities and accept responsibility 
    /// for maintaining consistency. For most scenarios, prefer the safe 
    /// <see cref="RTHandleMipChain{Tdata}"/> which provides the same basic functionality 
    /// without exposing unsafe operations.
    ///
    /// Redundant `IsValid` methods are provided for API consistency with the safe variant.
    /// </summary>
    /// <typeparam name="Tdata">
    /// Optional user data passed to the texture creation function, useful for passing context
    /// or resources needed during RenderGraph allocation.
    /// </typeparam>
    public class UnsafeRTHandleMipChain<Tdata> : UnsafeMipChain<RTHandle, Tdata>
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
    /// Represents an "unsafe" variant of <see cref="RTHandleMipChain{Tdata}"/>.
    /// 
    /// This class inherits from <see cref="UnsafeMipChain{Thandle,Tdata}"/> and exposes 
    /// advanced functionality not available in the safe <see cref="RTHandleMipChain{Tdata}"/>:
    /// - Checking ranges of mip handles for validity.
    /// - Copying subsets of chains or stacking multiple chains into one array.
    /// - Explicit control over handle array resizing and layout.
    ///
    /// Use this class only when you need these low-level capabilities and accept responsibility 
    /// for maintaining consistency. For most scenarios, prefer the safe 
    /// <see cref="RTHandleMipChain{Tdata}"/> which provides the same basic functionality 
    /// without exposing unsafe operations.
    ///
    /// Redundant `IsValid` methods are provided for API consistency with the safe variant.
    /// </summary>
    /// <typeparam name="Tdata">
    /// Optional user data passed to the texture creation function, useful for passing context
    /// or resources needed during RenderGraph allocation.
    /// </typeparam>
    public sealed class UnsafeRTHandleMipChain : UnsafeRTHandleMipChain<NoData>
    {
        /// <summary>
        /// Delegate for creating a handle for a mip level.
        /// </summary>
        /// <param name="handle">Reference to the current handle stored internally.</param>
        /// <param name="descriptor">Descriptor describing the texture to create.</param>
        /// <param name="mipLevel">Index of the mip level being created.</param>
        public delegate void CreateFunctionNoData(ref RTHandle handle, RenderTextureDescriptor descriptor, int mipLevel);

        /// <summary>
        /// Initializes a mip chain with a texture creation function.
        /// </summary>
        /// <param name="createFunc">Function to create each mip level.</param>
        public UnsafeRTHandleMipChain(CreateFunctionNoData createFunc)
            : base((ref RTHandle handle, RenderTextureDescriptor descriptor, int mipLevel, NoData _) =>
            {
                createFunc.Invoke(ref handle, descriptor, mipLevel);
            })
        { }
    }
}