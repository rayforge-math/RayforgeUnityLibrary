using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Rayforge.Core.Rendering.Passes;

namespace Rayforge.Core.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Base class for compute RenderGraph pass input/output configuration and dispatch metadata.
    /// Contains the destination texture(s) and the core compute pass metadata.
    /// </summary>
    /// <remarks>
    /// This class is declared as <c>partial</c> to allow projects to extend compute pass data
    /// with additional fields that should be present on all compute passes of this type
    /// (e.g. debug flags, frame indices, shared constants, or platform-specific parameters),
    /// without requiring inheritance or modification of the core framework.
    /// 
    /// Extensions should remain data-only and must not introduce execution logic.
    /// 
    /// Any per-dispatch logic should instead be implemented via <see cref="UpdateCallback"/>,
    /// which receives the fully populated pass data instance and allows binding of resources
    /// and constants without capturing external state.
    /// 
    /// This design follows Unity's RenderGraph principles:
    /// pass data is immutable during execution, value-type based, and free of per-frame
    /// heap allocations, ensuring predictable performance and zero-GC behavior.
    /// </remarks>
    /// <typeparam name="TDerived">
    /// Concrete pass data type (CRTP) enabling type-safe callbacks and extensions without allocations.
    /// </typeparam>
    public abstract partial class ComputePassData<TDerived> : PassDataBase<TDerived, ComputePassMeta, TextureMeta>
        where TDerived : ComputePassData<TDerived>
    {
        /// <summary>
        /// Optional callback invoked before dispatch to bind resources, set constants, etc.
        /// <para>
        /// <b>Performance note:</b> To avoid heap allocations per frame and to comply with RenderGraph's
        /// GC-free design, this callback should be assigned using a <c>static</c> lambda whenever possible.
        /// </para>
        /// <para>
        /// Using non-static lambdas or capturing local variables will create a closure object on the heap,
        /// which can result in per-frame allocations and temporary GC pressure.
        /// </para>
        /// <para>
        /// This design follows Unity's RenderGraph pattern, where all internal pass data and dispatch
        /// logic is value-type-based and heap-free, ensuring predictable frame timings and zero GC overhead.
        /// </para>
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