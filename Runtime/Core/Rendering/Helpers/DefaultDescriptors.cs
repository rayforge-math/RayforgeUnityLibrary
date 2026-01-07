using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.Rendering.Helpers
{
    public static class DefaultDescriptors
    {
        /// <summary>
        /// Returns a standard <see cref="RenderTextureDescriptor"/> for depth-only textures,
        /// suitable for Depth Pyramid / High-Z / History buffers that are written via ComputeShaders.
        /// </summary>
        /// <param name="width">Texture width in pixels.</param>
        /// <param name="height">Texture height in pixels.</param>
        /// <param name="enableRandomWrite">Whether the texture allows random write access by a ComputeShader. Default is <c>true</c>.</param>
        /// <param name="msaaSamples">Number of MSAA samples. Default is 1 (no MSAA). High-Z buffers typically should not use MSAA.</param>
        /// <returns>A configured <see cref="RenderTextureDescriptor"/>.</returns>
        public static RenderTextureDescriptor DepthBuffer(int width, int height, bool enableRandomWrite = true, int msaaSamples = 1)
        {
            return new RenderTextureDescriptor(width, height)
            {
                colorFormat = RenderTextureFormat.Depth,
                depthBufferBits = 32,
                dimension = TextureDimension.Tex2D,
                useMipMap = false,
                autoGenerateMips = false,
                msaaSamples = msaaSamples,
                sRGB = false,
                enableRandomWrite = enableRandomWrite,
                bindMS = false
            };
        }

        /// <summary>
        /// Returns a depth buffer descriptor matching the current screen resolution.
        /// </summary>
        /// <param name="width">Texture width in pixels.</param>
        /// <param name="height">Texture height in pixels.</param>
        public static RenderTextureDescriptor DepthBufferFullScreen(bool enableRandomWrite = true, int msaaSamples = 1)
            => DepthBuffer(Screen.width, Screen.height, enableRandomWrite, msaaSamples);
    }
}