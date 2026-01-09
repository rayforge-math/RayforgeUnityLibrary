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
        /// Optional callback invoked before rendering to configure material properties.
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