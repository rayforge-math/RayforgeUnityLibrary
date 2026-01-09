using Rayforge.Core.Diagnostics;
using Rayforge.Core.ShaderExtensions.Blitter;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Windows;

namespace Rayforge.Core.Utility.RenderGraphs.Rendering
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
        /// Useful for low-level full-screen operations, manual blits, or passes that
        /// require direct access to a native command buffer.
        /// </summary>
        /// <typeparam name="TPassData">Pass data type containing input/output textures and pass metadata.</typeparam>
        /// <param name="renderGraph">RenderGraph instance to which the pass is added.</param>
        /// <param name="passName">Name used for debugging and RenderGraph visualization.</param>
        /// <param name="passData">Pass data instance containing all input/output configuration.</param>
        public static void AddUnsafeRenderPass<TPassData>(RenderGraph renderGraph, string passName, TPassData passData)
            where TPassData : UnsafeRasterPassData<TPassData>, new()
        {
            CheckPreRequesites(renderGraph, passData);

            using (var builder = renderGraph.AddUnsafePass(passName, out TPassData data))
            {
                data.CopyFrom(passData);

                foreach (var input in data.PassInput)
                {
                    builder.UseTexture(input.handle, AccessFlags.Read);
                }
                builder.UseTexture(data.Destination, AccessFlags.Write);

                builder.SetRenderFunc(static (TPassData data, UnsafeGraphContext ctx) =>
                {
                    var passMeta = data.PassMeta;

                    MaterialPropertyBlock propertyBlock = passMeta.PropertyBlock;
                    if (propertyBlock == null)
                    {
                        s_PropertyBlock.Clear();
                        propertyBlock = s_PropertyBlock;
                    }

                    data.UpdateCallback?.Invoke(ctx.cmd, propertyBlock, data);

                    CommandBuffer unsafeCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    unsafeCmd.SetRenderTarget(data.Destination, 0, CubemapFace.Unknown, 0);

                    foreach (var input in data.PassInput)
                    {
                        propertyBlock.SetTexture(input.propertyId, input.handle);
                    }
                    propertyBlock.SetVector(BlitParameters.BlitScaleBiasId, Vector2.one);

                    unsafeCmd.DrawProcedural(Matrix4x4.identity, passMeta.Material, passMeta.PassId, MeshTopology.Triangles, 3, 1, propertyBlock);
                });
            }
        }

        /// <summary>
        /// Adds a standard raster RenderGraph pass.
        /// Handles render target attachments automatically.
        /// Recommended for most full-screen rendering unless low-level native buffer control is needed.
        /// </summary>
        /// <typeparam name="TPassData">Pass data type containing material and input/output state.</typeparam>
        /// <param name="renderGraph">RenderGraph instance to add the pass to.</param>
        /// <param name="passName">Display name for debugging and RenderGraph visualization.</param>
        /// <param name="passData">Pass data instance containing all input/output configuration.</param>
        public static void AddRasterRenderPass<TPassData>(RenderGraph renderGraph, string passName, TPassData passData)
            where TPassData : RasterPassData<TPassData>, new()
        {
            CheckPreRequesites(renderGraph, passData);

            using (var builder = renderGraph.AddRasterRenderPass(passName, out TPassData data))
            {
                data.CopyFrom(passData);

                foreach (var input in data.PassInput)
                {
                    builder.UseTexture(input.handle, AccessFlags.Read);
                }
                builder.SetRenderAttachment(data.Destination, 0, AccessFlags.Write);

                builder.SetRenderFunc(static (TPassData data, RasterGraphContext ctx) =>
                {
                    var passMeta = data.PassMeta;

                    MaterialPropertyBlock propertyBlock = passMeta.PropertyBlock;
                    if (propertyBlock == null)
                    {
                        s_PropertyBlock.Clear();
                        propertyBlock = s_PropertyBlock;
                    }

                    data.UpdateCallback?.Invoke(ctx.cmd, propertyBlock, data);

                    foreach (var input in data.PassInput)
                    {
                        propertyBlock.SetTexture(input.propertyId, input.handle);
                    }
                    propertyBlock.SetVector(BlitParameters.BlitScaleBiasId, Vector2.one);

                    ctx.cmd.DrawProcedural(Matrix4x4.identity, passMeta.Material, passMeta.PassId, MeshTopology.Triangles, 3, 1, propertyBlock);
                });
            }
        }

        /// <summary>
        /// Adds a compute RenderGraph pass.
        /// Automatically binds input textures, output texture, and dispatches the compute shader.
        /// </summary>
        /// <typeparam name="TPassData">Pass data type containing compute kernel metadata and textures.</typeparam>
        /// <param name="renderGraph">RenderGraph instance to add the pass to.</param>
        /// <param name="passName">Display name for debugging in the RenderGraph view.</param>
        /// <param name="passData">Pass data instance containing all input/output configuration.</param>
        public static void AddComputePass<TPassData>(RenderGraph renderGraph, string passName, TPassData passData)
            where TPassData : ComputePassData<TPassData>, new()
        {
            CheckPreRequesites(renderGraph, passData);

            using (var builder = renderGraph.AddComputePass(passName, out TPassData data))
            {
                data.CopyFrom(passData);

                foreach (var input in data.PassInput)
                {
                    builder.UseTexture(input.handle, AccessFlags.Read);
                }
                builder.UseTexture(data.Destination.handle, AccessFlags.Write);

                builder.SetRenderFunc(static (TPassData data, ComputeGraphContext ctx) =>
                {
                    var passMeta = data.PassMeta;

                    data.UpdateCallback?.Invoke(ctx.cmd, data);

                    foreach (var input in data.PassInput)
                    {
                        ctx.cmd.SetComputeTextureParam(passMeta.Shader, passMeta.KernelIndex, input.propertyId, input.handle);
                    }
                    var dest = data.Destination;
                    ctx.cmd.SetComputeTextureParam(passMeta.Shader, passMeta.KernelIndex, dest.propertyId, dest.handle);
                    
                    ctx.cmd.DispatchCompute(
                        passMeta.Shader,
                        passMeta.KernelIndex,
                        passMeta.ThreadGroupsX,
                        passMeta.ThreadGroupsY,
                        passMeta.ThreadGroupsZ
                    );
                });
            }
        }

        /// <summary>
        /// Verifies basic preconditions for adding a RenderGraph pass.
        /// </summary>
        /// <remarks>
        /// This method performs <b>development-time validation only</b>.
        /// The checked conditions represent programmer errors (invalid API usage)
        /// rather than recoverable runtime failures.
        ///
        /// Assertions are intentionally used instead of exceptions:
        /// - These conditions must always be true in a correct render setup.
        /// - Violations indicate a bug in pass construction or call order.
        /// - In non-development builds, the checks are compiled out to avoid overhead.
        /// </remarks>
        /// <typeparam name="TPassData">
        /// Type of the pass data being validated.
        /// </typeparam>
        /// <param name="renderGraph">
        /// The <see cref="RenderGraph"/> instance the pass is added to.
        /// Must not be <c>null</c>.
        /// </param>
        /// <param name="passData">
        /// The pass data instance containing input/output configuration and metadata.
        /// Must not be <c>null</c>.
        /// </param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private static void CheckPreRequesites<TPassData>(RenderGraph renderGraph, TPassData passData)
        {
            Assertions.NotNull(renderGraph, "RenderGraph must not be null.");
            Assertions.NotNull(passData, "PassData must not be null.");
        }
    }
}