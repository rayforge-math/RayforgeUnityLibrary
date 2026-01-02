using System;
using UnityEngine;

namespace Rayforge.ManagedResources.NativeMemory
{
    /// <summary>
    /// Descriptor for a 2D texture, containing resolution, pixel format,
    /// mipmap configuration, and sampling/filtering settings.
    /// Used as the configuration key for texture pooling.
    /// </summary>
    public struct Texture2dDescriptor : IEquatable<Texture2dDescriptor>
    {
        public int width;
        public int height;
        public TextureFormat colorFormat;
        public int mipCount;
        public bool linear;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;

        /// <summary>
        /// Compares all descriptor fields for equality.
        /// </summary>
        public bool Equals(Texture2dDescriptor other)
            => width == other.width
            && height == other.height
            && colorFormat == other.colorFormat
            && mipCount == other.mipCount
            && linear == other.linear
            && filterMode == other.filterMode
            && wrapMode == other.wrapMode;

        /// <summary>
        /// Object override to ensure proper equality handling when stored
        /// in collections such as <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </summary>
        public override bool Equals(object obj)
            => obj is Texture2dDescriptor other && Equals(other);

        /// <summary>
        /// Generates a stable hash from all fields so the descriptor can
        /// be safely used as a dictionary or hash set key.
        /// </summary>
        public override int GetHashCode()
            => (width, height, colorFormat, mipCount, linear, filterMode, wrapMode).GetHashCode();

        /// <summary>Equality operator for convenience.</summary>
        public static bool operator ==(Texture2dDescriptor left, Texture2dDescriptor right)
            => left.Equals(right);

        /// <summary>Inequality operator for convenience.</summary>
        public static bool operator !=(Texture2dDescriptor left, Texture2dDescriptor right)
            => !left.Equals(right);
    }
}