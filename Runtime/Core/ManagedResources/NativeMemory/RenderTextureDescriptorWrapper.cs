using System;
using UnityEngine;

namespace Rayforge.ManagedResources.NativeMemory
{
    /// <summary>
    /// Wrapper around Unity's <see cref="RenderTextureDescriptor"/> to provide
    /// value-based comparison and hashing for use in dictionaries and pools.
    /// </summary>
    public struct RenderTextureDescriptorWrapper : IEquatable<RenderTextureDescriptorWrapper>
    {
        /// <summary>The underlying descriptor.</summary>
        public RenderTextureDescriptor descriptor;

        /// <summary>
        /// Compares this wrapper with another wrapper for equality.
        /// </summary>
        public bool Equals(RenderTextureDescriptorWrapper other)
            => Equals(other.descriptor);

        /// <summary>
        /// Compares this wrapper with a raw <see cref="RenderTextureDescriptor"/>.
        /// </summary>
        public bool Equals(RenderTextureDescriptor other)
        {
            return
                other.width == descriptor.width &&
                other.height == descriptor.height &&
                other.colorFormat == descriptor.colorFormat &&
                other.depthBufferBits == descriptor.depthBufferBits &&
                other.dimension == descriptor.dimension &&
                other.volumeDepth == descriptor.volumeDepth &&
                other.msaaSamples == descriptor.msaaSamples &&
                other.useMipMap == descriptor.useMipMap &&
                other.autoGenerateMips == descriptor.autoGenerateMips &&
                other.enableRandomWrite == descriptor.enableRandomWrite &&
                other.useDynamicScale == descriptor.useDynamicScale &&
                other.sRGB == descriptor.sRGB &&
                other.bindMS == descriptor.bindMS;
        }

        /// <summary>
        /// Object equality override.
        /// </summary>
        public override bool Equals(object obj)
            => obj is RenderTextureDescriptorWrapper other && Equals(other);

        /// <summary>
        /// Creates a stable hash code from all relevant descriptor properties.
        /// </summary>
        public override int GetHashCode()
            => (
                descriptor.width,
                descriptor.height,
                descriptor.colorFormat,
                descriptor.depthBufferBits,
                descriptor.dimension,
                descriptor.volumeDepth,
                descriptor.msaaSamples,
                descriptor.useMipMap,
                descriptor.autoGenerateMips,
                descriptor.enableRandomWrite,
                descriptor.useDynamicScale,
                descriptor.sRGB,
                descriptor.bindMS
            ).GetHashCode();

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(RenderTextureDescriptorWrapper left, RenderTextureDescriptorWrapper right)
            => left.Equals(right);

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(RenderTextureDescriptorWrapper left, RenderTextureDescriptorWrapper right)
            => !left.Equals(right);
    }
}