using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Base class for raster RenderGraph pass input/output configuration and material binding.
    /// Contains the destination texture and the core pass metadata.
    /// </summary>
    /// <typeparam name="TMeta">The type of the pass metadata (e.g., <see cref="RasterPassMeta{TDerived}"/>).</typeparam>
    public abstract partial class RasterPassDataBase<TDerived, TMeta> : PassDataBase<TDerived, TMeta, TextureHandle>
        where TDerived : PassDataBase<TDerived, TMeta, TextureHandle>
        where TMeta : struct
    { }

    /// <summary>
    /// Raster pass data using <see cref="RasterPassMeta{TDerived}"/> as the pass metadata.
    /// Provides typed access to render target configuration and material-based rendering.
    /// </summary>
    /// <typeparam name="TDerived">The derived pass data type for type-safe callbacks.</typeparam>
    public abstract partial class RasterPassData<TDerived> : RasterPassDataBase<TDerived, RasterPassMeta<TDerived>>
        where TDerived : RasterPassDataBase<TDerived, RasterPassMeta<TDerived>>
    { }

    /// <summary>
    /// Raster pass data for execution with <see cref="UnsafeCommandBuffer"/>.
    /// Uses <see cref="UnsafeRasterPassMeta{TDerived}"/> for low-level command buffer access.
    /// </summary>
    /// <typeparam name="TDerived">The derived pass data type for type-safe callbacks.</typeparam>
    public abstract partial class UnsafeRasterPassData<TDerived> : RasterPassDataBase<TDerived, UnsafeRasterPassMeta<TDerived>>
        where TDerived : RasterPassDataBase<TDerived, UnsafeRasterPassMeta<TDerived>>
    { }
}