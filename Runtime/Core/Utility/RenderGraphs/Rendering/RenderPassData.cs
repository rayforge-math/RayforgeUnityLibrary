using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Base class for RenderGraph pass input/output configuration and material binding.
    /// </summary>
    public partial class RenderPassData<Tdata> : PassDataBase<Tdata, TextureHandle>
        where Tdata : struct
    {

    }

    public class SafeRenderPassData : RenderPassData<RasterPassMeta>
    {

    }

    public class UnsafeRenderPassData : RenderPassData<UnsafeRasterPassMeta>
    {

    }
}