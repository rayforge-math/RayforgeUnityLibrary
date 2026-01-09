using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Rayforge.Core.Rendering.Passes;

namespace Rayforge.Core.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Base class for raster RenderGraph pass input/output configuration and material binding.
    /// Contains the destination texture and the core pass metadata.
    /// </summary>
    /// <remarks>
    /// This class is declared as <c>partial</c> to allow projects to extend the pass data
    /// with additional fields that should be present on all raster passes of this type
    /// (e.g. debug flags, frame indices, or shared constants),
    /// without requiring inheritance or modification of the core framework.
    /// 
    /// Extensions should remain data-only and must not introduce execution logic.
    /// 
    /// Any per-dispatch logic should instead be implemented via <see cref="UpdateCallback"/>,
    /// which receives the fully populated pass data instance and allows binding of resources
    /// and constants without capturing external state.
    /// </remarks>
    /// <typeparam name="TDerived">
    /// Concrete pass data type (CRTP) enabling type-safe callbacks and extensions without allocations.
    /// </typeparam>
    /// <typeparam name="TCmd">
    /// Command buffer type used to record rendering commands for this pass.
    /// </typeparam>
    public abstract partial class RasterPassDataBase<TDerived, TCmd> : PassDataBase<TDerived, RasterPassMeta, TextureHandle>
        where TDerived : RasterPassDataBase<TDerived, TCmd>
        where TCmd : BaseCommandBuffer
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
        public Action<TCmd, MaterialPropertyBlock, TDerived> UpdateCallback { get; set; }
    }

    /// <summary>
    /// Raster pass data using <see cref="RasterPassMeta"/> and a safe <see cref="RasterCommandBuffer"/>.
    /// </summary>
    /// <typeparam name="TDerived">
    /// Concrete raster pass data type enabling type-safe callbacks without allocations.
    /// </typeparam>
    public abstract partial class RasterPassData<TDerived> : RasterPassDataBase<TDerived, RasterCommandBuffer>
        where TDerived : RasterPassData<TDerived>
    { }

    /// <summary>
    /// Raster pass data using <see cref="RasterPassMeta"/> with low-level
    /// <see cref="UnsafeCommandBuffer"/> access.
    /// </summary>
    /// <typeparam name="TDerived">
    /// Concrete raster pass data type enabling type-safe callbacks without allocations.
    /// </typeparam>
    public abstract partial class UnsafeRasterPassData<TDerived> : RasterPassDataBase<TDerived, UnsafeCommandBuffer> 
        where TDerived : UnsafeRasterPassData<TDerived>
    { }
}