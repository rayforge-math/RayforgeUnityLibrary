using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.RenderGraphExtensions.Rendering
{
    /// <summary>
    /// Represents a texture input bound to a pass, associating a shader property slot
    /// with a RenderGraph texture handle.
    /// </summary>
    public struct RenderPassTexture
    {
        public int propertyId;
        public TextureHandle handle;
    }
}