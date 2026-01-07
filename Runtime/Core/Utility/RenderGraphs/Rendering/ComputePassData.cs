using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Specialized pass data for compute passes.
    /// Provides convenience setters for input and destination textures using <see cref="TextureHandle"/>.
    /// </summary>
    public class ComputePassData<Tdata> : PassDataBase<ComputePassMeta<Tdata>, TextureMeta>
    {
        /// <summary>
        /// Sets the destination texture using a RenderGraph handle and optional property ID.
        /// </summary>
        /// <param name="handle">The texture handle to write into.</param>
        /// <param name="propertyId">Optional shader property ID to assign to the destination.</param>
        public void SetDestination(TextureHandle handle, int propertyId = 0)
            => SetDestination(new TextureMeta { handle = handle, propertyId = propertyId });

        /// <summary>
        /// Sets an input at the specified index using a texture handle and optional property ID.
        /// </summary>
        /// <param name="index">Input index (0-based).</param>
        /// <param name="handle">The texture handle to assign.</param>
        /// <param name="propertyId">Optional shader property ID to assign to the input.</param>
        public void SetInput(int index, TextureHandle handle, int propertyId = 0)
            => SetInput(index, new TextureMeta { handle = handle, propertyId = propertyId });

        /// <summary>
        /// Sets the first input (index 0) using a texture handle and optional property ID.
        /// </summary>
        /// <param name="handle">The texture handle to assign.</param>
        /// <param name="propertyId">Optional shader property ID to assign to the input.</param>
        public void SetInput(TextureHandle handle, int propertyId = 0)
            => SetInput(0, handle, propertyId);

        /// <summary>
        /// Returns the destination texture handle stored in this pass.
        /// </summary>
        public TextureHandle GetDestinationHandle()
            => Destination.handle;

        /// <summary>
        /// Returns the input texture handle at the specified index.
        /// </summary>
        /// <param name="index">Input index (0-based).</param>
        /// <returns>The <see cref="TextureHandle"/> stored at the input slot.</returns>
        public TextureHandle GetInputHandle(int index)
            => GetInput(index).handle;
    }
}