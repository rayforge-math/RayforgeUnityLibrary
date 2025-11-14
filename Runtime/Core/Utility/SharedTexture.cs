using UnityEngine;

namespace Rayforge.Utility.Rendering
{
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
