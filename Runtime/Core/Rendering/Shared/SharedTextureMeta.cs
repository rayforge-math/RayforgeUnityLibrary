using System;
using UnityEngine;

namespace Rayforge.Core.Rendering.Shared
{
    /// <summary>
    /// Immutable metadata describing a globally shared shader texture.
    /// Couples shader binding information with resource loading data.
    /// </summary>
    public readonly struct SharedTextureMeta : IEquatable<SharedTextureMeta>
    {
        /// <summary>Global shader property name (e.g. "_Rayforge_BlueNoise").</summary>
        public string ShaderPropertyName { get; }

        /// <summary>Shader property ID derived from <see cref="ShaderPropertyName"/>.</summary>
        public int ShaderPropertyId { get; }

        /// <summary>Resource path relative to the Resources folder.</summary>
        public string ResourceName { get; }

        /// <summary>
        /// Creates a new immutable shared texture descriptor.
        /// </summary>
        /// <param name="shaderPropertyName">
        /// Global shader property name used for binding.
        /// Must not be null or empty.
        /// </param>
        /// <param name="resourceName">
        /// Resource path used for loading the texture.
        /// Must not be null or empty.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="shaderPropertyName"/> or
        /// <paramref name="resourceName"/> is null or empty.
        /// </exception>
        public SharedTextureMeta(string shaderPropertyName, string resourceName)
        {
            if (string.IsNullOrEmpty(shaderPropertyName))
                throw new ArgumentException("Shader property name must not be null or empty.", nameof(shaderPropertyName));

            if (string.IsNullOrEmpty(resourceName))
                throw new ArgumentException("Resource name must not be null or empty.", nameof(resourceName));

            ShaderPropertyName = shaderPropertyName;
            ShaderPropertyId = Shader.PropertyToID(shaderPropertyName);
            ResourceName = resourceName;
        }

        /// <summary>
        /// Two metas are considered equal if they refer to the same shader property ID.
        /// </summary>
        public bool Equals(SharedTextureMeta other)
            => ShaderPropertyId == other.ShaderPropertyId;

        public override bool Equals(object obj)
        => obj is SharedTextureMeta other && Equals(other);

        /// <summary>
        /// Hash code based solely on the shader property ID.
        /// </summary>
        public override int GetHashCode()
            => ShaderPropertyId;
    }
}