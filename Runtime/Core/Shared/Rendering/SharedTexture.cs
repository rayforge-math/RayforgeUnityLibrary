using System;
using UnityEngine;

namespace Rayforge.Shared.Rendering
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
        /// identified by the given property ID. If a texture is already set under that
        /// property, the assignment is skipped to avoid redundant global state changes.
        /// </summary>
        /// <typeparam name="Ttex">
        /// The texture type to assign (Texture2D, RenderTexture, Texture3D, etc.).
        /// Must derive from <see cref="Texture"/>.
        /// </typeparam>
        /// <param name="propertyId">The integer shader property ID (see <see cref="Shader.PropertyToID"/>).</param>
        /// <param name="texture">The texture instance to assign.</param>
        /// <param name="forceOverwrite">
        /// Set to true to overwrite existing global textures even if one already exists.
        /// Default is false.
        /// </param>
        public static void Ensure<Ttex>(int propertyId, Ttex texture, bool forceOverwrite = false)
            where Ttex : Texture
        {
            if (texture == null)
            {
                Debug.LogWarning($"Tried to assign a NULL texture to global ID {propertyId}.");
                return;
            }

            var existing = Shader.GetGlobalTexture(propertyId);
            if (existing == null || forceOverwrite)
            {
                Shader.SetGlobalTexture(propertyId, texture);
                return;
            }
        }

        /// <summary>
        /// Retrieves the global texture assigned to the shader property with the given name.
        /// Returns null if none is assigned.
        /// </summary>
        public static Texture GetExisting(string property)
            => GetExisting(Shader.PropertyToID(property));

        /// <summary>
        /// Returns the currently bound global texture for the given shader property ID.
        /// Returns null if none is assigned.
        /// </summary>
        public static Texture GetExisting(int propertyId)
            => Shader.GetGlobalTexture(propertyId);

        /// <summary>
        /// Returns true if a global texture exists for the shader property with the given name.
        /// </summary>
        public static bool Exists(string property)
            => Exists(Shader.PropertyToID(property));

        /// <summary>
        /// Returns true if the given shader property ID already has a global texture assigned.
        /// </summary>
        public static bool Exists(int propertyId)
            => Shader.GetGlobalTexture(propertyId) != null;
    }
}
