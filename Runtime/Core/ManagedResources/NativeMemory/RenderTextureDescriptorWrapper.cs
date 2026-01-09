using Rayforge.Core.Diagnostics;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Wrapper around Unity's <see cref="RenderTextureDescriptor"/> to provide
    /// value-based comparison and hashing for use in dictionaries and pools.
    /// </summary>
    public struct RenderTextureDescriptorWrapper : IEquatable<RenderTextureDescriptorWrapper>
    {
        /// <summary>The underlying descriptor.</summary>
        private RenderTextureDescriptor descriptor;
        public RenderTextureDescriptor Descriptor
        {
            get => descriptor;
            set => CopyFrom(value);
        }

        /// <summary>The width of the render texture. Must be > 0.</summary>
        public int Width
        {
            get => descriptor.width;
            set
            {
                Assertions.AtLeastOne(value, "Width must be greater than zero.");
                descriptor.width = value;
            }
        }

        /// <summary>The height of the render texture. Must be > 0.</summary>
        public int Height
        {
            get => descriptor.height;
            set
            {
                Assertions.AtLeastOne(value, "Height must be greater than zero.");
                descriptor.height = value;
            }
        }

        /// <summary>Color format of the texture.</summary>
        public RenderTextureFormat ColorFormat
        {
            get => descriptor.colorFormat;
            set => descriptor.colorFormat = value;
        }

        /// <summary>Depth buffer bits (0, 16, 24, 32).</summary>
        public int DepthBufferBits
        {
            get => descriptor.depthBufferBits;
            set
            {
                Assertions.IsTrue(
                    value == 0 || value == 16 || value == 24 || value == 32,
                    "DepthBufferBits must be 0, 16, 24, or 32.");
                descriptor.depthBufferBits = value;
            }
        }

        /// <summary>Texture dimension.</summary>
        public TextureDimension Dimension
        {
            get => descriptor.dimension;
            set => descriptor.dimension = value;
        }

        /// <summary>MSAA sample count (1, 2, 4, 8).</summary>
        public int MSAASamples
        {
            get => descriptor.msaaSamples;
            set
            {
                Assertions.IsTrue(
                    value == 1 || value == 2 || value == 4 || value == 8,
                    "MSAASamples must be 1, 2, 4, or 8.");
                descriptor.msaaSamples = value;
            }
        }

        public bool UseMipMap { get => descriptor.useMipMap; set => descriptor.useMipMap = value; }
        public bool AutoGenerateMips { get => descriptor.autoGenerateMips; set => descriptor.autoGenerateMips = value; }
        public bool EnableRandomWrite { get => descriptor.enableRandomWrite; set => descriptor.enableRandomWrite = value; }
        public bool UseDynamicScale { get => descriptor.useDynamicScale; set => descriptor.useDynamicScale = value; }
        public bool SRGB { get => descriptor.sRGB; set => descriptor.sRGB = value; }
        public bool BindMS { get => descriptor.bindMS; set => descriptor.bindMS = value; }

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

        /// <summary>
        /// Copies all fields from another descriptor wrapper into this one.
        /// Validates width, height, depth, MSAA, etc. using assertions.
        /// </summary>
        /// <param name="other">The source descriptor wrapper to copy from.</param>
        public void CopyFrom(RenderTextureDescriptorWrapper other)
            => CopyFrom(other.descriptor);

        /// <summary>
        /// Copies all fields from another descriptor into this one.
        /// Validates width, height, depth, MSAA, etc. using assertions.
        /// </summary>
        /// <param name="other">The source descriptor to copy from.</param>
        public void CopyFrom(RenderTextureDescriptor desc)
        {
            Width = desc.width;
            Height = desc.height;
            ColorFormat = desc.colorFormat;
            DepthBufferBits = desc.depthBufferBits;
            Dimension = desc.dimension;
            MSAASamples = desc.msaaSamples;
            UseMipMap = desc.useMipMap;
            AutoGenerateMips = desc.autoGenerateMips;
            EnableRandomWrite = desc.enableRandomWrite;
            UseDynamicScale = desc.useDynamicScale;
            SRGB = desc.sRGB;
            BindMS = desc.bindMS;
        }
    }
}