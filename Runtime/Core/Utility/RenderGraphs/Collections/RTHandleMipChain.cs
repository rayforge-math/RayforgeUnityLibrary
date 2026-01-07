using Rayforge.Rendering.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Rayforge.Common;
using System;

namespace Rayforge.Utility.RenderGraphs.Collections
{
    /// <summary>
    /// Represents a chain of <see cref="RTHandle"/>s corresponding to mip levels of a texture
    /// specifically for use in RenderGraph passes. 
    /// 
    /// Unity's standard RenderTexture MipChain can be cumbersome in RenderGraph because:
    /// - Each mip level needs its own <see cref="RTHandle"/> allocation.
    /// - Copying or generating mips between levels requires explicit pass setup.
    /// - Automatic mip generation via standard RenderTexture is not directly supported in RenderGraph.
    /// 
    /// This structure simplifies the process by:
    /// - Creating all mip levels via a user-provided function.
    /// - Allowing optional mip map generation between handles in a RenderGraph-friendly way.
    /// - Providing easy access to individual mip handles and read-only spans for pass binding.
    /// </summary>
    /// <typeparam name="Tdata">
    /// Optional user data passed to the texture creation function, useful for passing context
    /// or resources needed during RenderGraph allocation.
    /// </typeparam>
    public class RTHandleMipChain<Tdata> : MipChain<RTHandle, Tdata>
    {
        /// <summary>
        /// Initializes a mip chain with a texture creation function.
        /// </summary>
        /// <param name="createFunc">Function to create each mip level.</param>
        public RTHandleMipChain(CreateFunction createFunc)
            : base(createFunc)
        { }
    }

    /// <summary>
    /// Represents a chain of <see cref="RTHandle"/>s corresponding to mip levels of a texture
    /// specifically for use in RenderGraph passes. 
    /// 
    /// Unity's standard RenderTexture MipChain can be cumbersome in RenderGraph because:
    /// - Each mip level needs its own <see cref="RTHandle"/> allocation.
    /// - Copying or generating mips between levels requires explicit pass setup.
    /// - Automatic mip generation via standard RenderTexture is not directly supported in RenderGraph.
    /// 
    /// This structure simplifies the process by:
    /// - Creating all mip levels via a user-provided function.
    /// - Allowing optional mip map generation between handles in a RenderGraph-friendly way.
    /// - Providing easy access to individual mip handles and read-only spans for pass binding.
    /// </summary>
    public sealed class RTHandleMipChain : RTHandleMipChain<NoData>
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
        public RTHandleMipChain(CreateFunctionNoData createFunc)
            : base((ref RTHandle handle, RenderTextureDescriptor descriptor, int mipLevel, NoData _) =>
            {
                createFunc.Invoke(ref handle, descriptor, mipLevel);
            })
        { }
    }
}