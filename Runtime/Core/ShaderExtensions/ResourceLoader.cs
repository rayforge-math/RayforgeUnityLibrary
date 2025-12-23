using Rayforge.Utility.Rendering;
using System;
using System.Collections.Generic;
using UnityEngine;

using static Rayforge.Utility.RuntimeCheck.Asserts;

namespace Rayforge.ShaderExtensions.ResourceLoader
{
    /// <summary>
    /// Immutable metadata describing a globally shared shader texture.
    /// Couples shader binding information with resource loading data.
    /// </summary>
    public sealed class SharedTextureMeta : IEquatable<SharedTextureMeta>
    {
        /// <summary>Global shader property name (e.g. "_Rayforge_BlueNoise").</summary>
        public string ShaderPropertyName { get; }

        /// <summary>Shader property ID derived from <see cref="ShaderPropertyName"/>.</summary>
        public int ShaderPropertyId { get; }

        /// <summary>Resource path relative to the Resources folder.</summary>
        public string ResourceName { get; }

        public SharedTextureMeta(string shaderPropertyName, string resourceName)
        {
            ShaderPropertyName = shaderPropertyName;
            ShaderPropertyId = Shader.PropertyToID(shaderPropertyName);
            ResourceName = resourceName;
        }

        /// <summary>
        /// Two metas are considered equal if they refer to the same shader property ID.
        /// </summary>
        public bool Equals(SharedTextureMeta other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return ShaderPropertyId == other.ShaderPropertyId;
        }

        public override bool Equals(object obj)
            => Equals(obj as SharedTextureMeta);

        /// <summary>
        /// Hash code based solely on the shader property ID.
        /// </summary>
        public override int GetHashCode()
            => ShaderPropertyId;
    }

    /// <summary>
    /// Provides access to shared global resources used across Rayforge shaders and in projects.
    /// Handles loading, validating, and globally registering textures that must be available
    /// to all rendering passes. All resources are loaded once and safely reused.
    /// </summary>
    public static class SharedTextureResources
    {
        /// <summary>
        /// Registry of all loaded shared textures, keyed by shader property metadata.
        /// The key identity is defined by the shader property ID.
        /// </summary>
        private static readonly Dictionary<SharedTextureMeta, Texture> k_RegisteredResources = new();

        private static readonly SharedTextureMeta k_BlueNoiseMeta = new("_Rayforge_BlueNoise", "BlueNoise512");
        private static readonly SharedTextureMeta k_Noise3DDetailMeta = new("_Rayforge_Noise3DDetail", "Noise3DDetail");
        private static readonly SharedTextureMeta k_Noise3DShapeMeta = new("_Rayforge_Noise3DShape", "Noise3DShape");
        private static readonly SharedTextureMeta k_NoiseDetailMeta = new("_Rayforge_NoiseDetail", "NoiseDetail512");
        private static readonly SharedTextureMeta k_NoiseShapeMeta = new("_Rayforge_NoiseShape", "NoiseShape512");

        /// <summary>
        /// Metadata for the blue noise texture used in e.g. ray offsets.
        /// </summary>
        public static SharedTextureMeta BlueNoiseMeta => k_BlueNoiseMeta;

        /// <summary>
        /// Metadata for the 3D detail noise texture used in volumetric effects.
        /// </summary>
        public static SharedTextureMeta Noise3DDetailMeta => k_Noise3DDetailMeta;

        /// <summary>
        /// Metadata for the 3D shape noise texture used in volumetric effects.
        /// </summary>
        public static SharedTextureMeta Noise3DShapeMeta => k_Noise3DShapeMeta;

        /// <summary>
        /// Metadata for the 2D detail noise texture used for high-frequency modulation.
        /// </summary>
        public static SharedTextureMeta NoiseDetailMeta => k_NoiseDetailMeta;

        /// <summary>
        /// Metadata for the 2D shape noise texture used for low-frequency modulation.
        /// </summary>
        public static SharedTextureMeta NoiseShapeMeta => k_NoiseShapeMeta;

        /// <summary>
        /// Loads the blue noise texture from the Resources folder (if not already loaded)
        /// and assigns it as a global shader texture.
        ///
        /// If a texture is already registered under the same shader property ID,
        /// it is reused (as long as it is a <see cref="Texture2D"/>).
        ///
        /// This function is safe to call multiple times; the texture is only loaded once.
        /// </summary>
        public static void LoadBlueNoise()
            => LoadAndRegisterTexture<Texture2D>(k_BlueNoiseMeta);

        /// <summary>
        /// Loads the 3D detail noise texture from the Resources folder (if not already loaded)
        /// and assigns it as a global shader texture.
        ///
        /// If a texture is already registered under the same shader property ID,
        /// it is reused (as long as it is a <see cref="Texture3D"/>).
        ///
        /// This function is safe to call multiple times; the texture is only loaded once.
        /// </summary>
        public static void LoadNoise3DDetail()
            => LoadAndRegisterTexture<Texture3D>(k_Noise3DDetailMeta);

        /// <summary>
        /// Loads the 3D shape noise texture from the Resources folder (if not already loaded)
        /// and assigns it as a global shader texture.
        ///
        /// If a texture is already registered under the same shader property ID,
        /// it is reused (as long as it is a <see cref="Texture3D"/>).
        ///
        /// This function is safe to call multiple times; the texture is only loaded once.
        /// </summary>
        public static void LoadNoise3DShape()
            => LoadAndRegisterTexture<Texture3D>(k_Noise3DShapeMeta);

        /// <summary>
        /// Loads the 2D detail noise texture from the Resources folder (if not already loaded)
        /// and assigns it as a global shader texture.
        ///
        /// If a texture is already registered under the same shader property ID,
        /// it is reused (as long as it is a <see cref="Texture2D"/>).
        ///
        /// This function is safe to call multiple times; the texture is only loaded once.
        /// </summary>
        public static void LoadNoiseDetail()
            => LoadAndRegisterTexture<Texture2D>(k_NoiseDetailMeta);

        /// <summary>
        /// Loads the 2D shape noise texture from the Resources folder (if not already loaded)
        /// and assigns it as a global shader texture.
        ///
        /// If a texture is already registered under the same shader property ID,
        /// it is reused (as long as it is a <see cref="Texture2D"/>).
        ///
        /// This function is safe to call multiple times; the texture is only loaded once.
        /// </summary>
        public static void LoadNoiseShape()
            => LoadAndRegisterTexture<Texture2D>(k_NoiseShapeMeta);

        /// <summary>
        /// Loads and registers a shared texture resource described by the given metadata.
        /// Safe to call multiple times.
        /// </summary>
        /// <typeparam name="T">
        /// Type of the texture to load (e.g. <see cref="Texture2D"/>, <see cref="Texture3D"/>).
        /// </typeparam>
        /// <param name="meta">
        /// Immutable metadata describing how the texture is bound and loaded.
        /// The shader property ID defines the identity of the resource.
        /// </param>
        private static void LoadAndRegisterTexture<T>(SharedTextureMeta meta)
            where T : Texture
        {
            // Already loaded and cached locally
            if (k_RegisteredResources.TryGetValue(meta, out var cached))
            {
                Validate(
                    cached is T,
                    (_) => _,
                    $"Shared texture '{meta.ShaderPropertyName}' was already registered " +
                    $"but has incompatible type {cached.GetType().Name} (expected {typeof(T).Name}).");

                return;
            }

            // Try to reuse an existing global shader texture
            var existing = SharedTexture.GetExisting(meta.ShaderPropertyId);

            if (existing != null)
            {
                Validate(
                    existing is T,
                    (_) => _,
                    $"Global texture bound to '{meta.ShaderPropertyName}' is of type " +
                    $"{existing.GetType().Name}, expected {typeof(T).Name}.");

                k_RegisteredResources.Add(meta, existing);
                return;
            }

            // Load from Resources
            var loaded = Resources.Load<T>(
                ResourcePaths.TextureResourceFolder + meta.ResourceName);

            Validate(
                loaded,
                tex => tex != null,
                $"Shared texture '{meta.ResourceName}' could not be loaded.");

            // Register globally and cache locally
            SharedTexture.Ensure(meta.ShaderPropertyId, loaded);
            k_RegisteredResources.Add(meta, loaded);
        }
    }
}