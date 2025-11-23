using UnityEngine;

using static Rayforge.Utility.RuntimeCheck.Asserts;

namespace Rayforge.Utility.Rendering
{
    /// <summary>
    /// Provides access to shared global resources used across Rayforge shaders and other
    /// systems. This class is responsible for loading, validating, and globally registering
    /// textures that must be available to multiple rendering passes.
    ///
    /// All resources managed here are loaded once and exposed as global shader properties.
    /// Repeated calls to the loading functions are safe, as they only initialize resources
    /// on first use.
    /// </summary>
    public static class SharedResources
    {
        private const string k_TexturesFolder = "Textures/";

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
        /// and assigns it as a global shader texture. The texture is validated before use.
        /// Calling this method multiple times is safe: the texture is only loaded once.
        /// </summary>
        public static void LoadBlueNoise()
        {
            if (s_BlueNoiseTexture == null)
            {
                s_BlueNoiseTexture = Resources.Load<Texture2D>(k_TexturesFolder + k_BlueNoiseResourceName);
            }

            Validate(
                s_BlueNoiseTexture, 
                _ => _ != null,
                "Blue Noise texture " + nameof(s_BlueNoiseTexture) + " is null.");

            SharedTexture.Ensure(k_BlueNoiseTextureId, s_BlueNoiseTexture);
        }
    }

    /// <summary>
    /// Utility class for managing shared global textures in shaders.
    /// Ensures that a given texture is set as a global shader property only once,
    /// avoiding redundant assignments.
    /// </summary>
    public static class SharedTexture
    {
        /// <summary>
        /// Ensures that the specified texture is assigned to the global shader property
        /// identified by the given name. If the property is already set, it does nothing.
        /// </summary>
        /// <typeparam name="Ttex">
        /// The texture type to assign (e.g., <see cref="Texture2D"/>, <see cref="RenderTexture"/> or <see cref="Texture3D"/>).
        /// Must derive from <see cref="Texture"/>.
        /// </typeparam>
        /// <param name="property">The name of the global shader property (e.g., "_MainTex").</param>
        /// <param name="texture">The texture instance to assign.</param>
        public static void Ensure<Ttex>(string property, Ttex texture)
            where Ttex : Texture
            => Ensure(Shader.PropertyToID(property), texture);

        /// <summary>
        /// Ensures that the specified texture is assigned to the global shader property
        /// identified by the given property ID. If the property is already set, it does nothing.
        /// </summary>
        /// <typeparam name="Ttex">
        /// The texture type to assign (e.g., <see cref="Texture2D"/>, <see cref="RenderTexture"/> or <see cref="Texture3D"/>).
        /// Must derive from <see cref="Texture"/>.
        /// </typeparam>
        /// <param name="propertyId">The integer ID of the global shader property (use <see cref="Shader.PropertyToID"/> to obtain it).</param>
        /// <param name="texture">The texture instance to assign.</param>
        public static void Ensure<Ttex>(int propertyId, Ttex texture)
            where Ttex : Texture
        {
            // Check if the global shader property already has a texture assigned
            var existing = Shader.GetGlobalTexture(propertyId);

            // Only assign the texture if nothing is set yet
            if (existing == null)
            {
                Shader.SetGlobalTexture(propertyId, texture);
            }
        }
    }
}
