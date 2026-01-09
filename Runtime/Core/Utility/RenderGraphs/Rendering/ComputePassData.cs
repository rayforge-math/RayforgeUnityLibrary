using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Rayforge.Rendering.Passes;

namespace Rayforge.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Specialized pass data for compute passes.
    /// Provides convenience setters for input and destination textures using <see cref="TextureHandle"/>.
    /// </summary>
    public abstract partial class ComputePassData<TDerived> : PassDataBase<TDerived, ComputePassMeta, TextureMeta>
        where TDerived : PassDataBase<TDerived, ComputePassMeta, TextureMeta>
    {
        /// <summary>
        /// Core compute metadata (shader, kernel, thread groups).
        /// </summary>
        public ComputePassMeta Meta { get; set; }

        /// <summary>
        /// Optional callback invoked before dispatch to bind resources, set constants, etc.
        /// </summary>
        public Action<ComputeCommandBuffer, TDerived> UpdateCallback { get; set; }

        /// <summary>
        /// Sets the destination texture using a RenderGraph handle and optional shader property ID.
        /// </summary>
        /// <param name="handle">The texture handle to write into.</param>
        /// <param name="propertyId">Optional shader property ID to bind the texture to.</param>
        public void SetDestination(TextureHandle handle, int propertyId = 0)
            => SetDestination(new TextureMeta { handle = handle, propertyId = propertyId });

        /// <summary>
        /// Sets an input texture at the specified index using a handle and optional shader property ID.
        /// </summary>
        /// <param name="index">Zero-based input index.</param>
        /// <param name="handle">The texture handle to assign.</param>
        /// <param name="propertyId">Optional shader property ID to bind the texture to.</param>
        public void SetInput(int index, TextureHandle handle, int propertyId = 0)
            => SetInput(index, new TextureMeta { handle = handle, propertyId = propertyId });

        /// <summary>
        /// Sets the first input texture (index 0) using a handle and optional shader property ID.
        /// </summary>
        /// <param name="handle">The texture handle to assign.</param>
        /// <param name="propertyId">Optional shader property ID to bind the texture to.</param>
        public void SetInput(TextureHandle handle, int propertyId = 0)
            => SetInput(0, handle, propertyId);

        /// <summary>
        /// Gets the destination texture handle stored in this pass.
        /// </summary>
        /// <returns>The destination <see cref="TextureHandle"/>.</returns>
        public TextureHandle GetDestinationHandle()
            => Destination.handle;

        /// <summary>
        /// Gets the input texture handle at the specified index.
        /// </summary>
        /// <param name="index">Zero-based input index.</param>
        /// <returns>The <see cref="TextureHandle"/> at the specified input slot.</returns>
        public TextureHandle GetInputHandle(int index)
            => GetInput(index).handle;
    }
}