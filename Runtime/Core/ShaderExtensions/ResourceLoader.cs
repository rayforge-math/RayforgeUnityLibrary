using Rayforge.Utility.Rendering;
using UnityEngine;

using static Rayforge.Utility.RuntimeCheck.Asserts;

namespace Rayforge.ShaderExtensions.ResourceLoader
{
    /// <summary>
    /// Provides access to shared global resources used across Rayforge shaders and in projects.
    /// Handles loading, validating, and globally registering textures that must be available
    /// to all rendering passes. All resources are loaded once and safely reused.
    /// </summary>
    public static class SharedResources
    {
        private const string k_BlueNoiseTextureName = "_Rayforge_BlueNoise";
        /// <summary>
        /// Gets the global shader property name used to bind the blue noise texture.
        /// </summary>
        public const string BlueNoiseTextureName = k_BlueNoiseTextureName;

        private static readonly int k_BlueNoiseTextureId = Shader.PropertyToID(k_BlueNoiseTextureName);
        /// <summary>
        /// Gets the shader property ID for the blue noise global texture.
        /// </summary>
        public static int BlueNoiseTextureId => k_BlueNoiseTextureId;

        private const string k_BlueNoiseResourceName = "BlueNoise512";
        private static Texture2D s_BlueNoiseTexture;

        /// <summary>
        /// Loads the blue noise texture from the Resources folder (if not already loaded)
        /// and assigns it as a global shader texture. If a texture is already registered
        /// under the same global property ID, it is reused (as long as it is a Texture2D).
        ///
        /// This function is safe to call multiple times; the texture is only loaded once.
        /// </summary>
        public static void LoadBlueNoise()
        {
            if (s_BlueNoiseTexture != null)
                return;

            var existing = SharedTexture.GetExisting(k_BlueNoiseTextureId);

            if (existing is Texture2D existingTex2D)
            {
                s_BlueNoiseTexture = existingTex2D;
                return;
            }
            else if (existing != null)
            {
                Debug.LogWarning(
                    $"SharedResources: Existing texture for '{BlueNoiseTextureName}' " +
                    $"is not a Texture2D (type: {existing.GetType().Name}). Replacing it.");
            }

            s_BlueNoiseTexture = Resources.Load<Texture2D>(ResourcePaths.TextureResourceFolder + k_BlueNoiseResourceName);

            Validate(
                s_BlueNoiseTexture,
                tex => tex != null,
                $"Blue noise texture '{k_BlueNoiseResourceName}' could not be loaded.");

            // Register globally
            SharedTexture.Ensure(k_BlueNoiseTextureId, s_BlueNoiseTexture);
        }
    }
}