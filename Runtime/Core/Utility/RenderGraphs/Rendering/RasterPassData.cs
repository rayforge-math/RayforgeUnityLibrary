using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Base class for Raster RenderGraph pass input/output configuration and material binding.
    /// Contains the destination texture and the core pass metadata.
    /// </summary>
    /// <typeparam name="Tmeta">The type of the core pass metadata (e.g., RasterPassMeta).</typeparam>
    public partial class RasterPassDataBase<Tmeta> : PassDataBase<Tmeta, TextureHandle>
        where Tmeta : struct
    { }

    /// <summary>
    /// Strongly typed Raster pass using <see cref="RasterPassMeta"/> as the core pass metadata.
    /// </summary>
    public class RasterPassData<Tdata> : RasterPassDataBase<RasterPassMeta<Tdata>>
    { }

    /// <summary>
    /// Strongly typed Raster pass for execution with <see cref="UnsafeCommandBuffer"/>.
    /// </summary>
    public class UnsafeRasterPassData<Tdata> : RasterPassDataBase<UnsafeRasterPassMeta<Tdata>>
    { }
}