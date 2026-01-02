using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

using Rayforge.ShaderExtensions.Blitter;

namespace Rayforge.RenderGraphExtensions.Rendering
{
    /// <summary>
    /// Provides helper functions for adding procedural full-screen blit passes
    /// to the RenderGraph API.
    /// </summary>
    public static class RenderPassRecorder
    {
        private static MaterialPropertyBlock s_PropertyBlock = new();

        /// <summary>
        /// Delegate used to set up pass data before execution.
        /// </summary>
        public delegate void PassDataUpdate<TpassData>(TpassData passData) where TpassData : RenderPassData;
        /// <summary>
        /// Delegate used to configure additional rendering parameters during pass execution.
        /// </summary>
        public delegate void RenderDataUpdate<Tcmd>(Tcmd cmd, MaterialPropertyBlock mpb) where Tcmd : BaseCommandBuffer;

        /// <summary>
        /// Adds a custom RenderGraph pass executed using <see cref="UnsafeCommandBuffer"/>.
        /// This is useful for low-level full-screen operations, manual blits or passes that require
        /// direct command buffer access.
        /// </summary>
        /// <typeparam name="TpassData">Pass data type used to configure inputs, outputs and material state.</typeparam>
        /// <param name="renderGraph">RenderGraph instance the pass is added to.</param>
        /// <param name="passName">Name used for debugging and RenderGraph visualization.</param>
        /// <param name="passDataUpdate">
        /// Delegate used to configure the pass input/output state before execution.  
        /// Called when the pass is created.
        /// </param>
        /// <param name="renderDataUpdate">
        /// Optional delegate invoked during execution, allowing additional command buffer setup
        /// before drawing (e.g., keyword setup, material property block adjustments).
        /// </param>
        public static void AddUnsafeRenderPass<TpassData>(RenderGraph renderGraph, string passName, PassDataUpdate<TpassData> passDataUpdate, RenderDataUpdate<UnsafeCommandBuffer> renderDataUpdate = null)
            where TpassData : RenderPassData, new()
        {
            using (var builder = renderGraph.AddUnsafePass(passName, out TpassData data))
            {
                passDataUpdate.Invoke(data);

                foreach (var input in data.PassInput)
                {
                    builder.UseTexture(input.handle, AccessFlags.Read);
                }
                builder.UseTexture(data.Destination, AccessFlags.Write);

                builder.SetRenderFunc((TpassData data, UnsafeGraphContext ctx) =>
                {
                    MaterialPropertyBlock propertyBlock = data.PropertyBlock;
                    if (propertyBlock == null)
                    {
                        s_PropertyBlock.Clear();
                        propertyBlock = s_PropertyBlock;
                    }

                    renderDataUpdate?.Invoke(ctx.cmd, propertyBlock);

                    CommandBuffer unsafeCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    unsafeCmd.SetRenderTarget(data.Destination, 0, CubemapFace.Unknown, 0);

                    foreach (var input in data.PassInput)
                    {
                        propertyBlock.SetTexture(input.propertyId, input.handle);
                    }
                    propertyBlock.SetVector(BlitParameters.BlitScaleBiasId, Vector2.one);

                    unsafeCmd.DrawProcedural(Matrix4x4.identity, data.Material, data.PassId, MeshTopology.Triangles, 3, 1, propertyBlock);
                });
            }
        }

        /// <summary>
        /// Adds a standard raster RenderGraph pass. Render target attachments are handled automatically.
        /// Recommended for most full-screen rendering operations unless low-level native buffer control is required.
        /// </summary>
        /// <typeparam name="TpassData">Pass data type used to configure material and input/output state.</typeparam>
        /// <param name="renderGraph">RenderGraph instance to add the pass to.</param>
        /// <param name="passName">Display name used in the RenderGraph debug view.</param>
        /// <param name="passDataUpdate">
        /// Delegate called when the pass is created, used to configure input textures and target output.
        /// </param>
        /// <param name="renderDataUpdate">
        /// Optional delegate invoked during pass execution to customize material property state.
        /// </param>
        public static void AddRasterRenderPass<TpassData>(RenderGraph renderGraph, string passName, PassDataUpdate<TpassData> passDataUpdate, RenderDataUpdate<RasterCommandBuffer> renderDataUpdate = null)
            where TpassData : RenderPassData, new()
        {
            using (var builder = renderGraph.AddRasterRenderPass(passName, out TpassData data))
            {
                passDataUpdate.Invoke(data);

                foreach (var input in data.PassInput)
                {
                    builder.UseTexture(input.handle, AccessFlags.Read);
                }
                builder.SetRenderAttachment(data.Destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((TpassData data, RasterGraphContext ctx) =>
                {
                    MaterialPropertyBlock propertyBlock = data.PropertyBlock;
                    if (propertyBlock == null)
                    {
                        s_PropertyBlock.Clear();
                        propertyBlock = s_PropertyBlock;
                    }

                    renderDataUpdate?.Invoke(ctx.cmd, propertyBlock);

                    foreach (var input in data.PassInput)
                    {
                        propertyBlock.SetTexture(input.propertyId, input.handle);
                    }
                    propertyBlock.SetVector(BlitParameters.BlitScaleBiasId, Vector2.one);

                    ctx.cmd.DrawProcedural(Matrix4x4.identity, data.Material, data.PassId, MeshTopology.Triangles, 3, 1, propertyBlock);
                });
            }
        }
    }
}