using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.Core.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Represents a texture input bound to a pass, associating a shader property slot
    /// with a RenderGraph texture handle.
    /// </summary>
    public struct TextureMeta
    {
        public int propertyId;
        public TextureHandle handle;
    }
}