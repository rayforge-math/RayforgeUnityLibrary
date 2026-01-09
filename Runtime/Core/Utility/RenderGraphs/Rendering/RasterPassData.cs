using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Rayforge.Rendering.Passes;

namespace Rayforge.Utility.RenderGraphs.Rendering
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
    /// </remarks>
    /// <typeparam name="TMeta">The type of the pass metadata (e.g., <see cref="RasterPassMeta"/>).</typeparam>
    public abstract partial class RasterPassDataBase<TDerived, TMeta, TCmd> : PassDataBase<TDerived, TMeta, TextureHandle>
        where TDerived : PassDataBase<TDerived, TMeta, TextureHandle>
        where TMeta : struct
        where TCmd : BaseCommandBuffer
    {
        /// <summary>
        /// Core raster metadata (material, pass index/name, property block).
        /// </summary>
        public RasterPassMeta Meta { get; set; }

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
    /// Raster pass data using <see cref="RasterPassMeta"/> as the pass metadata.
    /// Provides typed access to render target configuration and material-based rendering.
    /// </summary>
    /// <typeparam name="TDerived">The derived pass data type for type-safe callbacks.</typeparam>
    public abstract partial class RasterPassData<TDerived> : RasterPassDataBase<TDerived, RasterPassMeta, RasterCommandBuffer>
        where TDerived : RasterPassData<TDerived>
    { }

    /// <summary>
    /// Raster pass data for execution with <see cref="UnsafeCommandBuffer"/>.
    /// Uses <see cref="RasterPassMeta"/> for low-level command buffer access.
    /// </summary>
    /// <typeparam name="TDerived">The derived pass data type for type-safe callbacks.</typeparam>
    public abstract partial class UnsafeRasterPassData<TDerived> : RasterPassDataBase<TDerived, RasterPassMeta, UnsafeCommandBuffer>
        where TDerived : UnsafeRasterPassData<TDerived>
    { }
}