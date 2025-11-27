using UnityEngine;

namespace Rayforge.ShaderExtensions.Blitter
{
    /// <summary>
    /// Provides shader property identifiers for common blit parameters used when drawing
    /// full-screen procedural triangles inside RenderGraph passes.
    /// </summary>
    public static class BlitParameters
    {
        private const string k_BlitTextureName = "_BlitTexture";
        /// <summary>Shader property name used to bind the source texture for a blit pass.</summary>
        public static string BlitTextureName => k_BlitTextureName;
        private const string k_BlitDestinationName = "_BlitDestination";
        /// <summary>Shader property name used to bind the destination texture for a blit pass.</summary>
        public static string BlitDestinationName => k_BlitDestinationName;
        private const string k_BlitScaleBiasName = "_BlitScaleBias";
        /// <summary>Shader property name used to provide scale and bias for sampling operations.</summary>
        public static string BlitScaleBiasName => k_BlitScaleBiasName;

        private static readonly int k_BlitTextureId = Shader.PropertyToID(k_BlitTextureName);
        /// <summary>Property ID for the blit texture.</summary>
        public static int BlitTextureId => k_BlitTextureId;
        private static readonly int k_BlitDestinationId = Shader.PropertyToID(k_BlitDestinationName);
        /// <summary>Property ID for the blit destination texture.</summary>
        public static int BlitDestinationId => k_BlitDestinationId;
        private static readonly int k_BlitScaleBiasId = Shader.PropertyToID(k_BlitScaleBiasName);
        /// <summary>Property ID for the blit scale/bias vector.</summary>
        public static int BlitScaleBiasId => k_BlitScaleBiasId;
    }
}