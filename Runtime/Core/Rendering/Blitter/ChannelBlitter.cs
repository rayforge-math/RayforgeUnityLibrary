using Rayforge.ManagedResources.NativeMemory;
using Rayforge.ShaderExtensions.Blitter;
using UnityEngine;
using Rayforge.ManagedResources.NativeMemory.Helpers;
using Rayforge.Common;

using static UnityEngine.Resources;

namespace Rayforge.Rendering.Blitter
{
    /// <summary>
    /// Utility class for performing channel-wise blits from a source texture
    /// to a destination render target. Supports both compute shader and rasterization pipelines.
    /// </summary>
    public static class ChannelBlitter
    {
        /// <summary>
        /// Determines which pipeline to use for the blit operation.
        /// </summary>
        public enum BlitType
        {
            /// <summary>Use a compute shader for the blit.</summary>
            Compute,
            /// <summary>Use the rasterization pipeline (full-screen triangle / DrawProcedural).</summary>
            Raster
        }

        /// <summary>
        /// Name of the compute shader inside the Resources folder.
        /// Used for channel-wise blitting via compute shader.
        /// Loaded through <c>Shader.Find()</c> or <c>Resources.Load()</c>.
        /// </summary>
        private const string k_ComputeBlitShaderName = "ComputeChannelBlitter";
        /// <summary>
        /// Name of the raster (non-compute) shader inside the Resources folder.
        /// Used for standard GPU rasterization-based channel blitting.
        /// Loaded through <c>Shader.Find()</c> or <c>Resources.Load()</c>.
        /// </summary>
        private const string k_RasterBlitShaderName = "RasterChannelBlitter";

        /// <summary>Shader property ID for the red channel mapping.</summary>
        public static int ChannelRId => k_ChannelRId;
        private static readonly int k_ChannelRId = Shader.PropertyToID("_R");
        /// <summary>Shader property ID for the green channel mapping.</summary>
        public static int ChannelGId => k_ChannelGId;
        private static readonly int k_ChannelGId = Shader.PropertyToID("_G");
        /// <summary>Shader property ID for the blue channel mapping.</summary>
        public static int ChannelBId => k_ChannelBId;
        private static readonly int k_ChannelBId = Shader.PropertyToID("_B");
        /// <summary>Shader property ID for the alpha channel mapping.</summary>
        public static int ChannelAId => k_ChannelAId;
        private static readonly int k_ChannelAId = Shader.PropertyToID("_A");

        /// <summary>Shader property ID for the blit parameter vector.</summary>
        public static int BlitParamsId => k_BlitParamsId;
        private static readonly int k_BlitParamsId = Shader.PropertyToID("_BlitParams");

        /// <summary>Shader property ID for the entire parameter cbuffer block <see cref="ChannelBlitterParams"/>.</summary>
        public static int ChannelBlitterParamsId => k_ChannelBlitterParamsId;
        private static readonly int k_ChannelBlitterParamsId = Shader.PropertyToID("_ChannelBlitterParams");

        /// <summary>Compute shader used for compute pipeline blits.</summary>
        private static readonly ComputeShader k_ComputeBlitShader;
        public static ComputeShader ComputeBlitShader => k_ComputeBlitShader;

        /// <summary>Material used for rasterization pipeline blits (fullscreen triangle).</summary>
        private static readonly Material k_RasterBlitMaterial;
        public static Material RasterBlitMaterial => k_RasterBlitMaterial;

        /// <summary>Reusable MaterialPropertyBlock for raster blits.</summary>
        private static readonly MaterialPropertyBlock k_PropertyBlock;

        /// <summary>
        /// Static constructor: loads shaders and initializes the raster blit material and property block.
        /// </summary>
        static ChannelBlitter()
        {
            k_ComputeBlitShader = Load<ComputeShader>(k_ComputeBlitShaderName);
            var shader = Shader.Find(ResourcePaths.ShaderNamespace + k_RasterBlitShaderName);
            k_RasterBlitMaterial = new Material(shader);
            k_PropertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Blits a source texture to a destination render target using the specified pipeline type.
        /// </summary>
        /// <param name="source">Source texture to read from.</param>
        /// <param name="dest">Destination render texture.</param>
        /// <param name="type">Pipeline type (Compute or Raster) to use.</param>
        /// <param name="param">Channel mapping and rectangle parameters.</param>
        public static void Blit(Texture source, RenderTexture dest, BlitType type, ChannelBlitterParams param)
        {
            switch (type)
            {
                case BlitType.Raster:
                    RasterBlit(source, dest, param);
                    break;
                case BlitType.Compute:
                    ComputeBlit(source, dest, param);
                    break;
            }
        }

        /// <summary>
        /// Performs a rasterization blit using a pre-filled MaterialPropertyBlock.
        /// </summary>
        /// <param name="source">Source texture.</param>
        /// <param name="dest">Destination render target.</param>
        /// <param name="mpb">MaterialPropertyBlock containing channel and blit parameters.</param>
        public static void RasterBlit(Texture source, RenderTexture dest, MaterialPropertyBlock mpb)
        {
            mpb.SetTexture(BlitParameters.BlitTextureId, source);
            Graphics.SetRenderTarget(dest);
            Graphics.DrawProcedural(k_RasterBlitMaterial, new Bounds(Vector2.zero, Vector2.one), MeshTopology.Triangles, 3, 1, null, mpb);
            Graphics.SetRenderTarget(null);
        }

        /// <summary>
        /// Rasterization blit using ChannelBlitterParams cbuffer.
        /// </summary>
        /// <param name="source">Source texture.</param>
        /// <param name="dest">Destination render target.</param>
        /// <param name="param">Cbuffer expected to contain <see cref="ChannelBlitterParams"/>.</param>
        /// <param name="offset"><see cref="ChannelBlitterParams"/> struct offset within cbuffer.</param>
        public static void RasterBlit(Texture source, RenderTexture dest, ManagedComputeBuffer param, int offset = 0)
        {
            k_PropertyBlock.SetCBuffer(k_ChannelBlitterParamsId, param, offset);
            RasterBlit(source, dest, k_PropertyBlock);
        }

        /// <summary>
        /// Rasterization blit using ChannelBlitterParams struct.
        /// Converts parameters into a MaterialPropertyBlock and invokes the raster blit.
        /// </summary>
        /// <param name="source">Source texture.</param>
        /// <param name="dest">Destination render target.</param>
        /// <param name="param">Channel mapping and offset/size rectangle.</param>
        private static void RasterBlit(Texture source, RenderTexture dest, ChannelBlitterParams param)
        {
            k_PropertyBlock.SetVector(k_BlitParamsId, new Vector4(param.offset.x, param.offset.y, param.size.x, param.size.y));
            k_PropertyBlock.SetInteger(k_ChannelRId, (int)param.R);
            k_PropertyBlock.SetInteger(k_ChannelGId, (int)param.G);
            k_PropertyBlock.SetInteger(k_ChannelBId, (int)param.B);
            k_PropertyBlock.SetInteger(k_ChannelAId, (int)param.A);

            RasterBlit(source, dest, k_PropertyBlock);
        }

        /// <summary>
        /// Performs a compute-based blit operation using <see cref="ChannelBlitterParams"/>.
        /// This path is intended for non-interpolated, non-normalized pixel data where
        /// bit-accurate transfer is required (e.g., bitfields, masks, integer-packed channels) or 
        /// as a slightly more performant blit operation where no interpolation is needed without
        /// invoking the entire rasterizer pipeline.
        ///
        /// The compute pipeline ensures that the source values are copied 1:1 without 
        /// filtering or normalization, allowing precise preservation of bit-level information.
        /// </summary>
        /// <param name="source">Source texture containing raw pixel data.</param>
        /// <param name="dest">Destination render texture.</param>
        /// <param name="param">Channel mapping and offset/size rectangle.</param>
        public static void ComputeBlit(Texture source, RenderTexture dest, ChannelBlitterParams param)
        {
            k_ComputeBlitShader.SetVector(k_BlitParamsId, new Vector4(param.offset.x, param.offset.y, param.size.x, param.size.y));
            k_ComputeBlitShader.SetInt(k_ChannelRId, (int)param.R);
            k_ComputeBlitShader.SetInt(k_ChannelGId, (int)param.G);
            k_ComputeBlitShader.SetInt(k_ChannelBId, (int)param.B);
            k_ComputeBlitShader.SetInt(k_ChannelAId, (int)param.A);

            k_ComputeBlitShader.SetTexture(0, BlitParameters.BlitTextureId, source);
            k_ComputeBlitShader.SetTexture(0, BlitParameters.BlitDestinationId, dest);

            var numGroups = new Vector2Int(Mathf.CeilToInt(param.size.x / 8.0f), Mathf.CeilToInt(param.size.y / 8.0f));
            k_ComputeBlitShader.Dispatch(0, numGroups.x, numGroups.y, 1);
        }
    }
}
