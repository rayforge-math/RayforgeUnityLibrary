using Rayforge.Core.Diagnostics;
using System;
using UnityEngine;

namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Descriptor for a 2D texture, containing resolution, pixel format,
    /// mipmap configuration, and sampling/filtering settings.
    /// Used as the configuration key for texture pooling.
    /// </summary>
    public struct Texture2dDescriptor : IEquatable<Texture2dDescriptor>
    {
        private int width;
        private int height;
        private TextureFormat colorFormat;
        private int mipCount;
        private bool linear;
        private FilterMode filterMode;
        private TextureWrapMode wrapMode;

        /// <summary>Texture width in pixels. Must be > 0.</summary>
        public int Width
        {
            get => width;
            set
            {
                Assertions.AtLeastOne(value, "Width must be greater than zero.");
                width = value;
            }
        }

        /// <summary>Texture height in pixels. Must be > 0.</summary>
        public int Height
        {
            get => height;
            set
            {
                Assertions.AtLeastOne(value, "Height must be greater than zero.");
                height = value;
            }
        }

        /// <summary>Pixel format of the texture.</summary>
        public TextureFormat ColorFormat
        {
            get => colorFormat;
            set => colorFormat = value;
        }

        /// <summary>Number of mip levels. Must be >= 1.</summary>
        public int MipCount
        {
            get => mipCount;
            set
            {
                Assertions.AtLeastOne(value, "MipCount must be at least 1.");
                mipCount = value;
            }
        }

        /// <summary>Linear color space flag.</summary>
        public bool Linear { get => linear; set => linear = value; }

        /// <summary>Filtering mode for texture sampling.</summary>
        public FilterMode FilterMode { get => filterMode; set => filterMode = value; }

        /// <summary>Wrap mode for texture addressing.</summary>
        public TextureWrapMode WrapMode { get => wrapMode; set => wrapMode = value; }

        /// <summary>
        /// Copies all fields from another descriptor, using property setters (assertions applied).
        /// </summary>
        public void CopyFrom(Texture2dDescriptor other)
        {
            Width = other.Width;
            Height = other.Height;
            ColorFormat = other.ColorFormat;
            MipCount = other.MipCount;
            Linear = other.Linear;
            FilterMode = other.FilterMode;
            WrapMode = other.WrapMode;
        }

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

        public override bool Equals(object obj)
            => obj is Texture2dDescriptor other && Equals(other);

        public override int GetHashCode()
            => (width, height, colorFormat, mipCount, linear, filterMode, wrapMode).GetHashCode();

        public static bool operator ==(Texture2dDescriptor left, Texture2dDescriptor right)
            => left.Equals(right);

        public static bool operator !=(Texture2dDescriptor left, Texture2dDescriptor right)
            => !left.Equals(right);
    }
}