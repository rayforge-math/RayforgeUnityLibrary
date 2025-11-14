using Codice.CM.Common;
using Rayforge.ManagedResources.NativeMemory;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayforge.Utility.Blitter
{
    public enum Channel : uint
    {
        R = 0,
        G = 1,
        B = 2,
        A = 3,
        None = 4
    }

    public enum BlitType
    {
        Compute,
        Raster
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ChannelBlitterParams
    {
        public Channel R;
        public Channel G;
        public Channel B;
        public Channel A;
        public Vector2 offset;
        public Vector2 size;
    }

    public static class ChannelBlitter
    {
        private const string k_ComputeBlitShaderName = "ComputeChannelBlitter";
        private const string k_RasterBlitShaderName = "RasterChannelBlitter";

        public static readonly int k_BlitSourceId = Shader.PropertyToID("_BlitSource");
        public static int BlitSourceId => k_BlitSourceId;
        public static readonly int k_BlitDestId = Shader.PropertyToID("_BlitDest");
        public static int BlitDestId => k_BlitDestId;

        public static readonly int k_ChannelRId = Shader.PropertyToID("_R");
        public static readonly int k_ChannelGId = Shader.PropertyToID("_G");
        public static readonly int k_ChannelBId = Shader.PropertyToID("_B");
        public static readonly int k_ChannelAId = Shader.PropertyToID("_A");

        public static readonly int k_BlitParamsId = Shader.PropertyToID("_BlitParams");
        public static int BlitParamsId => k_BlitParamsId;

        private static readonly ComputeShader k_ComputeBlitShader;
        public static ComputeShader ComputeBlitShader => k_ComputeBlitShader;
        private static readonly Material k_RasterBlitMaterial;
        public static Material RasterBlitMaterial => k_RasterBlitMaterial;

        private static readonly MaterialPropertyBlock k_PropertyBlock;

        static ChannelBlitter()
        {
            k_ComputeBlitShader = Resources.Load<ComputeShader>(k_ComputeBlitShaderName);
            var shader = Shader.Find("Rayforge/" + k_RasterBlitShaderName);
            k_RasterBlitMaterial = new Material(shader);
            k_PropertyBlock = new();
        }

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

        public static void RasterBlit(Texture source, RenderTexture dest, MaterialPropertyBlock mpb)
        {
            mpb.SetTexture(k_BlitSourceId, source);
            Graphics.SetRenderTarget(dest);
            Graphics.DrawProcedural(k_RasterBlitMaterial, new Bounds(Vector2.zero, Vector2.one), MeshTopology.Triangles, 3, 1, null, mpb);
            Graphics.SetRenderTarget(null);
        }

        private static void RasterBlit(Texture source, RenderTexture dest, ChannelBlitterParams param)
        {
            k_PropertyBlock.SetVector(k_BlitParamsId, new Vector4(param.offset.x, param.offset.y, param.size.x, param.size.y));

            k_PropertyBlock.SetInteger(k_ChannelRId, (int)param.R);
            k_PropertyBlock.SetInteger(k_ChannelGId, (int)param.G);
            k_PropertyBlock.SetInteger(k_ChannelBId, (int)param.B);
            k_PropertyBlock.SetInteger(k_ChannelAId, (int)param.A);

            RasterBlit(source, dest, k_PropertyBlock);
        }

        public static void ComputeBlit(Texture source, RenderTexture dest, ChannelBlitterParams param)
        {
            k_ComputeBlitShader.SetVector(k_BlitParamsId, new Vector4(param.offset.x, param.offset.y, param.size.x, param.size.y));

            k_ComputeBlitShader.SetInt(k_ChannelRId, (int)param.R);
            k_ComputeBlitShader.SetInt(k_ChannelGId, (int)param.G);
            k_ComputeBlitShader.SetInt(k_ChannelBId, (int)param.B);
            k_ComputeBlitShader.SetInt(k_ChannelAId, (int)param.A);

            k_ComputeBlitShader.SetTexture(0, k_BlitSourceId, source);
            k_ComputeBlitShader.SetTexture(0, k_BlitDestId, dest);

            var numGroups = new Vector2Int(Mathf.CeilToInt(param.size.x / 8.0f), Mathf.CeilToInt(param.size.y / 8.0f));
            k_ComputeBlitShader.Dispatch(0, numGroups.x, numGroups.y, 1);
        }
    }
}
