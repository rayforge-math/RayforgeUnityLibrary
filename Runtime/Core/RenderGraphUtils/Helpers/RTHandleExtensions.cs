using UnityEngine.Rendering;

using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.RenderGraphUtils.Helpers
{
    /// <summary>
    /// Extension methods for <see cref="RTHandle"/> to simplify integration with RenderGraph.
    /// </summary>
    public static class RTHandleExtensions
    {
        /// <summary>
        /// Imports a persistent <see cref="RTHandle"/> into a <see cref="RenderGraph"/> as a <see cref="TextureHandle"/>.
        /// This allows using a pre-existing RTHandle (e.g., ping-pong history or reprojection target) within a RenderGraph pass.
        /// </summary>
        /// <param name="handle">The <see cref="RTHandle"/> to import.</param>
        /// <param name="renderGraph">The <see cref="RenderGraph"/> instance where the texture will be used.</param>
        /// <returns>A <see cref="TextureHandle"/> representing the imported <see cref="RTHandle"/> inside the render graph.</returns>
        public static TextureHandle ToRenderGraphHandle(this RTHandle handle, RenderGraph renderGraph)
            => renderGraph.ImportTexture(handle);
    }
}