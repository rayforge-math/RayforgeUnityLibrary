using Rayforge.ShaderExtensions.Blitter;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Provides helper functions for recording RenderGraph passes in a way that follows
    /// Unity's intended RenderGraph usage patterns.
    ///
    /// <para>
    /// This implementation closely follows the design principles described in Unity's
    /// official RenderGraph documentation:
    /// https://docs.unity3d.com/6000.3/Documentation/Manual/urp/render-graph-write-render-pass.html
    /// </para>
    ///
    /// <para>
    /// In particular, it intentionally avoids heap allocations during render graph execution.
    /// Unity explicitly designed the RenderGraph API so that all per-pass state is stored in
    /// a <c>passData</c> object, which is then passed as a parameter to the render function.
    /// This avoids capturing external variables in lambdas, which would otherwise cause
    /// hidden heap allocations and GC pressure.
    /// </para>
    ///
    /// <para>
    /// As recommended by Unity, all data required by the render function is copied into
    /// the pass data struct ahead of time, and the render function operates exclusively
    /// on its parameters (<c>passData</c> and <c>context</c>).
    /// </para>
    /// </summary>
    public static class RenderPassRecorder
    {
        private static MaterialPropertyBlock s_PropertyBlock = new();

        /// <summary>
        /// Adds a custom RenderGraph pass executed using <see cref="UnsafeCommandBuffer"/>.
        /// This is useful for low-level full-screen operations, manual blits or passes that require
        /// direct command buffer access.
        /// </summary>
        /// <typeparam name="TpassData">Pass data type used to configure inputs, outputs and material state.</typeparam>
        /// <param name="renderGraph">RenderGraph instance the pass is added to.</param>
        /// <param name="passName">Name used for debugging and RenderGraph visualization.</param>
        public static void AddUnsafeRenderPass<TpassData>(RenderGraph renderGraph, string passName, TpassData passData)
            where TpassData : RenderPassDataBase<UnsafeRasterPassMeta>, new()
        {
            using (var builder = renderGraph.AddUnsafePass(passName, out TpassData data))
            {
                data.CopyFrom(passData);

                foreach (var input in data.PassInput)
                {
                    builder.UseTexture(input.handle, AccessFlags.Read);
                }
                builder.UseTexture(data.Destination, AccessFlags.Write);

                builder.SetRenderFunc((TpassData data, UnsafeGraphContext ctx) =>
                {
                    var dispatchMeta = data.AdditionalData.DispatchMeta;

                    MaterialPropertyBlock propertyBlock = dispatchMeta.PropertyBlock;
                    if (propertyBlock == null)
                    {
                        s_PropertyBlock.Clear();
                        propertyBlock = s_PropertyBlock;
                    }

                    data.AdditionalData.UpdateCallback?.Invoke(ctx.cmd, propertyBlock);

                    CommandBuffer unsafeCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    unsafeCmd.SetRenderTarget(data.Destination, 0, CubemapFace.Unknown, 0);

                    foreach (var input in data.PassInput)
                    {
                        propertyBlock.SetTexture(input.propertyId, input.handle);
                    }
                    propertyBlock.SetVector(BlitParameters.BlitScaleBiasId, Vector2.one);

                    unsafeCmd.DrawProcedural(Matrix4x4.identity, dispatchMeta.Material, dispatchMeta.PassId, MeshTopology.Triangles, 3, 1, propertyBlock);
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
        public static void AddRasterRenderPass<TpassData>(RenderGraph renderGraph, string passName, TpassData passData)
            where TpassData : RenderPassDataBase<RasterPassMeta>, new()
        {
            using (var builder = renderGraph.AddRasterRenderPass(passName, out TpassData data))
            {
                data.CopyFrom(passData);

                foreach (var input in data.PassInput)
                {
                    builder.UseTexture(input.handle, AccessFlags.Read);
                }
                builder.SetRenderAttachment(data.Destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((TpassData data, RasterGraphContext ctx) =>
                {
                    var dispatchMeta = data.AdditionalData.DispatchMeta;

                    MaterialPropertyBlock propertyBlock = dispatchMeta.PropertyBlock;
                    if (propertyBlock == null)
                    {
                        s_PropertyBlock.Clear();
                        propertyBlock = s_PropertyBlock;
                    }

                    data.AdditionalData.UpdateCallback?.Invoke(ctx.cmd, propertyBlock);

                    foreach (var input in data.PassInput)
                    {
                        propertyBlock.SetTexture(input.propertyId, input.handle);
                    }
                    propertyBlock.SetVector(BlitParameters.BlitScaleBiasId, Vector2.one);

                    ctx.cmd.DrawProcedural(Matrix4x4.identity, dispatchMeta.Material, dispatchMeta.PassId, MeshTopology.Triangles, 3, 1, propertyBlock);
                });
            }
        }

        /// <summary>
        /// Adds a compute RenderGraph pass.
        /// Automatically sets up input textures, output texture, and dispatches the compute shader using
        /// the specified <see cref="ComputePassMeta"/>.
        /// </summary>
        /// <typeparam name="TpassData">Type of pass data used to configure inputs, outputs, and material state.</typeparam>
        /// <param name="renderGraph">RenderGraph instance to add the pass to.</param>
        /// <param name="passName">Display name used in RenderGraph debug view.</param>
        public static void AddComputePass<TpassData>(RenderGraph renderGraph, string passName, TpassData passData)
            where TpassData : RenderPassDataBase<ComputePassMeta>, new()
        {
            using(var builder = renderGraph.AddComputePass(passName, out TpassData data))
            {
                data.CopyFrom(passData);

                foreach (var input in data.PassInput)
                {
                    builder.UseTexture(input.handle, AccessFlags.Read);
                }
                builder.UseTexture(data.Destination, AccessFlags.Write);

                builder.SetRenderFunc((TpassData data, ComputeGraphContext ctx) =>
                {
                    data.AdditionalData.UpdateCallback?.Invoke(ctx.cmd);

                    var dispatchMeta = data.AdditionalData.DispatchMeta;
                    ctx.cmd.DispatchCompute(dispatchMeta.Shader, dispatchMeta.KernelIndex, dispatchMeta.ThreadGroupsX, dispatchMeta.ThreadGroupsY, dispatchMeta.ThreadGroupsZ);
                });
            }
        }
    }
}