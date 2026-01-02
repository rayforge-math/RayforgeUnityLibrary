using System;
using UnityEngine.Rendering.RenderGraphModule;

using Rayforge.Rendering.MipChain;

namespace Rayforge.RenderGraphExtensions.MipChain
{
    /// <summary>
    /// Represents a chain of <see cref="TextureHandle"/>s corresponding to mip levels of a texture
    /// specifically for use in RenderGraph passes. 
    /// 
    /// Unity's standard RenderTexture MipChain can be cumbersome in RenderGraph because:
    /// - Each mip level needs its own <see cref="TextureHandle"/> allocation.
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
    public sealed class TextureHandleMipChain<Tdata> : MipChain<TextureHandle, Tdata>
        where Tdata : class
    {
        /// <summary>
        /// Initializes a mip chain with a texture creation function.
        /// </summary>
        /// <param name="createFunc">Function to create each mip level.</param>
        public TextureHandleMipChain(CreateFunction createFunc)
            : base(createFunc)
        { }

        /// <summary>
        /// Returns true if all mip handles in the chain are valid.
        /// </summary>
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
        /// Returns true if the specified mip handle is valid.
        /// </summary>
        /// <param name="mip">Index of the mip level to check.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="mip"/> is out of bounds.</exception>
        public bool IsValid(int mip)
        {
            if (mip < 0 || mip >= Handles.Count)
                throw new ArgumentOutOfRangeException(nameof(mip), $"Mip index must be between 0 and {Handles.Count - 1}.");

            return Handles[mip].IsValid();
        }
    }
}