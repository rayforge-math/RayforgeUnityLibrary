using System;
using UnityEngine;

namespace Rayforge.Shared.Rendering
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

        /// <summary>
        /// Hash code based solely on the shader property ID.
        /// </summary>
        public override int GetHashCode()
            => ShaderPropertyId;
    }
}