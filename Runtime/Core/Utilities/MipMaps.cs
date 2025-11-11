using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

using static Rayforge.CustomUtility.RuntimeCheck.Asserts;

namespace Rayforge.CustomUtility.GraphicsBuffer
{
    /// <summary>
    /// Extension methods for <see cref="Vector2Int"/> to simplify common resolution operations.
    /// </summary>
    public static class ResolutionExtensions
    {
        /// <summary>
        /// Checks whether two <see cref="Vector2Int"/> values match exactly in both X and Y.
        /// </summary>
        /// <param name="lhs">The first resolution to compare.</param>
        /// <param name="rhs">The second resolution to compare.</param>
        /// <returns>True if both X and Y components are equal; otherwise false.</returns>
        public static bool Matches(this Vector2Int lhs, Vector2Int rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y;
        }
    }

    /// <summary>
    /// Delegate used to generate resolution for a mip level from a base resolution.
    /// </summary>
    /// <param name="mipLevel">The mip level index (0 is full resolution).</param>
    /// <param name="baseRes">The base resolution.</param>
    /// <returns>The resolution for the given mip level.</returns>
    public delegate Vector2Int MipCreateFunc(int mipLevel, Vector2Int baseRes);

    /// <summary>
    /// Manages a chain of <see cref="RenderTextureDescriptor"/> for multiple mip levels, with automatic resolution and format updates.
    /// </summary>
    public struct RenderTextureDescriptorMipChain
    {
        /// <summary>Function used to generate mip resolutions for each level.</summary>
        private readonly MipCreateFunc k_MipCreateFunc;

        /// <summary>Array of descriptors for each mip level.</summary>
        private RenderTextureDescriptor[] m_Descriptors;

        /// <summary>Read-only access to the mip level descriptors.</summary>
        public IReadOnlyList<RenderTextureDescriptor> Descriptors => m_Descriptors;

        /// <summary>Access a specific mip level descriptor by index.</summary>
        /// <param name="index">The mip level index.</param>
        /// <returns>The <see cref="RenderTextureDescriptor"/> at the given index.</returns>
        public RenderTextureDescriptor this[int index] => m_Descriptors[index];

        private RenderTextureFormat m_Format;
        /// <summary>Format of all descriptors in the chain.</summary>
        public RenderTextureFormat Format
        {
            get => m_Format;
            set => UpdateFormat(value);
        }

        public int Length => m_Descriptors?.Length ?? 0;
        /// <summary>Total number of mip levels.</summary>
        public int MipCount
        {
            get => Length;
            set => UpdateMipCount(value);
        }

        private Vector2Int m_Resolution;
        /// <summary>Width and height of the full resolution (mip 0).</summary>
        public Vector2Int Resolution
        {
            get => m_Resolution;
            set => UpdateResolution(value);
        }

        /// <summary>Width of the base resolution.</summary>
        public int Width
        {
            get => m_Resolution.x;
            set => Resolution = new Vector2Int { x = value, y = m_Resolution.y };
        }

        /// <summary>Height of the base resolution.</summary>
        public int Height
        {
            get => m_Resolution.y;
            set => Resolution = new Vector2Int { x = m_Resolution.x, y = value };
        }

        /// <summary>
        /// Creates a mip chain with the given resolution, optional mip generation function, mip count, and format.
        /// </summary>
        /// <param name="width">Base width.</param>
        /// <param name="height">Base height.</param>
        /// <param name="createFunc">Optional custom mip generation function.</param>
        /// <param name="mipCount">Number of mip levels.</param>
        /// <param name="format">Render texture format.</param>
        public RenderTextureDescriptorMipChain(int width, int height, MipCreateFunc createFunc = null, int mipCount = 1, RenderTextureFormat format = RenderTextureFormat.Default)
        {
            var resolution = new Vector2Int(width, height);
            ValidateResolution(resolution);
            ValidateMipCount(mipCount);

            k_MipCreateFunc = createFunc ?? DefaultMipCreate;
            m_Format = format;
            m_Resolution = resolution;
            m_Descriptors = Array.Empty<RenderTextureDescriptor>();

            InitMipChain(mipCount);
        }

        /// <summary>
        /// Updates the resolution of the mip chain and recalculates all mip levels using the mip creation function.
        /// </summary>
        /// <param name="resolution">The new base resolution.</param>
        private void UpdateResolution(Vector2Int resolution)
        {
            if (!Resolution.Matches(resolution))
            {
                ValidateResolution(resolution);

                m_Resolution = resolution;
                for (int i = 0; i < m_Descriptors.Length; ++i)
                {
                    var mipRes = k_MipCreateFunc.Invoke(i, m_Resolution);

                    m_Descriptors[i].width = mipRes.x;
                    m_Descriptors[i].height = mipRes.y;
                }
            }
        }

        /// <summary>
        /// Updates the number of mip levels in the chain and reinitializes the descriptors if the count has changed.
        /// </summary>
        /// <param name="mipCount">The new number of mip levels.</param>
        private void UpdateMipCount(int mipCount)
        {
            if (MipCount != mipCount)
            {
                ValidateMipCount(mipCount);
                InitMipChain(mipCount);
            }
        }

        /// <summary>
        /// Updates the format of all descriptors in the mip chain.
        /// </summary>
        /// <param name="format">The new <see cref="RenderTextureFormat"/> to use for all mip levels.</param>
        private void UpdateFormat(RenderTextureFormat format)
        {
            if(m_Format != format)
            {
                m_Format = format;
                for (int i = 0; i < m_Descriptors.Length; ++i)
                {
                    m_Descriptors[i].colorFormat = format;
                }
            }
        }

        /// <summary>
        /// Initializes the mip chain descriptors with the current resolution and format.
        /// </summary>
        /// <param name="mipCount">The number of mip levels to initialize.</param>
        private void InitMipChain(int mipCount)
        {
            if (m_Descriptors.Length != mipCount)
            {
                m_Descriptors = new RenderTextureDescriptor[mipCount];
            }

            for (int i = 0; i < m_Descriptors.Length; ++i)
            {
                var resolution = k_MipCreateFunc(i, m_Resolution);
                m_Descriptors[i] = new RenderTextureDescriptor(resolution.x, resolution.y, m_Format, 0);
            }
        }

        /// <summary>
        /// Default mip creation function that calculates the resolution of a mip level from the base resolution.
        /// </summary>
        /// <param name="mipLevel">The mip level to calculate.</param>
        /// <param name="resolution">The base resolution.</param>
        /// <returns>A <see cref="Vector2Int"/> representing the mip resolution.</returns>
        public static Vector2Int DefaultMipCreate(int mipLevel, Vector2Int resolution)
        {
            return new Vector2Int { x = DefaultMipCreate(mipLevel, resolution.x), y = DefaultMipCreate(mipLevel, resolution.y) };
        }

        /// <summary>
        /// Default mip creation function for a single dimension.
        /// </summary>
        /// <param name="mipLevel">The mip level to calculate.</param>
        /// <param name="resolution">The base resolution for the dimension.</param>
        /// <returns>The calculated mip resolution.</returns>
        private static int DefaultMipCreate(int mipLevel, int resolution)
        {
            return Mathf.Max(1, resolution >> mipLevel);
        }

        /// <summary>
        /// Validates that the mip count is greater than zero.
        /// </summary>
        /// <param name="mipCount">The mip count to validate.</param>
        private static void ValidateMipCount(int mipCount)
        {
            const string error = "MipCount must be greater than 0";
            Validate(mipCount, (val) => { return val > 0; }, error);
        }

        /// <summary>
        /// Validates that the given resolution has positive width and height.
        /// </summary>
        /// <param name="resolution">The resolution to validate.</param>
        private static void ValidateResolution(Vector2Int resolution)
        {
            const string error = "resolution must be greater than 0";
            Validate(resolution, (val) => { return resolution.x > 0 && resolution.y > 0; }, error);
        }
    }

    /// <summary>
    /// Represents a chain of TextureHandles, typically corresponding to mip levels of a texture.
    /// </summary>
    /// <typeparam name="Tdata">Optional user data passed to texture creation function.</typeparam>
    public struct TextureHandleMipChain<Tdata> where Tdata : class
    {
        /// <summary>
        /// Function signature for creating a texture handle for a mip level.
        /// </summary>
        public delegate TextureHandle TextureCreateFunction(RenderTextureDescriptor descriptor, int mipLevel, Tdata data = null);

        /// <summary>
        /// Function signature for generating mip maps between two texture handles.
        /// </summary>
        public delegate void GenerateMipMapsFunction(TextureHandle src, TextureHandle dest, int mipLevel);

        private TextureHandle[] m_Handles;
        private TextureCreateFunction m_CreateFunc;

        /// <summary>Read-only access to texture handles.</summary>
        public IReadOnlyList<TextureHandle> Handles => m_Handles;

        /// <summary>Access a specific mip level handle by index.</summary>
        /// <param name="index">The mip level index.</param>
        /// <returns>The <see cref="RenderTextureDescriptor"/> at the given index.</returns>
        public TextureHandle this[int index] => m_Handles[index];

        /// <summary>Total number of mip levels.</summary>
        public int MipCount => m_Handles?.Length ?? 0;

        /// <summary>
        /// Initializes a mip chain with a texture creation function.
        /// </summary>
        /// <param name="createFunc">Function to create each mip level.</param>
        public TextureHandleMipChain(TextureCreateFunction createFunc)
        {
            ValidateDelegate(createFunc);
            m_CreateFunc = createFunc;
            m_Handles = Array.Empty<TextureHandle>();
        }

        /// <summary>
        /// Creates all mip levels from a descriptor chain.
        /// </summary>
        /// <param name="descriptors">The descriptor chain containing mip resolutions.</param>
        /// <param name="data">Optional user data.</param>
        /// <returns>True if all mip levels were successfully created.</returns>
        public bool Create(RenderTextureDescriptorMipChain descriptors, Tdata data = null)
            => Create(descriptors, 0, descriptors.MipCount, data);

        /// <summary>
        /// Creates only the first mip level.
        /// </summary>
        /// <param name="descriptors">The descriptor chain.</param>
        /// <param name="data">Optional user data.</param>
        /// <returns>True if creation succeeded.</returns>
        public bool CreateFirst(RenderTextureDescriptorMipChain descriptors, Tdata data = null)
            => Create(descriptors, 0, 1, data);

        /// <summary>
        /// Creates a range of mip levels starting from <paramref name="startMip"/> and creating <paramref name="count"/> levels.
        /// </summary>
        /// <param name="descriptors">Descriptor chain for mip resolutions.</param>
        /// <param name="startMip">Index of first mip to create.</param>
        /// <param name="count">Number of mip levels to create.</param>
        /// <param name="data">Optional user data.</param>
        /// <returns>True if all mip levels in the range were successfully created.</returns>
        public bool Create(RenderTextureDescriptorMipChain descriptors, int startMip, int count, Tdata data = null)
        {
            ValidateMipChainDescriptors(descriptors);
            ValidateMipCount(descriptors, startMip, count);

            startMip = Mathf.Clamp(startMip, 0, descriptors.MipCount - 1);
            count = Mathf.Clamp(count, 1, descriptors.MipCount - startMip);
            int endMip = startMip + count;

            if (m_Handles.Length != descriptors.MipCount)
            {
                var oldHandles = m_Handles;
                m_Handles = new TextureHandle[descriptors.Length];
                if (oldHandles != null && startMip > 0)
                    Array.Copy(oldHandles, m_Handles, Math.Min(startMip, oldHandles.Length));
            }

            for (int i = startMip; i < endMip; ++i)
            {
                if (!Create(i, descriptors[i], data))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a single <see cref="TextureHandle"/> at the specified index using the provided descriptor and optional user data.
        /// </summary>
        /// <param name="index">The index in the handle array where the texture will be created.</param>
        /// <param name="descriptor">The <see cref="RenderTextureDescriptor"/> describing the texture to create.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        /// <returns>True if the created texture handle is valid; otherwise false.</returns>
        private bool Create(int index, RenderTextureDescriptor descriptor, Tdata data = null)
        {
            m_Handles[index] = m_CreateFunc.Invoke(descriptor, index, data);
            return m_Handles[index].IsValid();
        }

        /// <summary>
        /// Returns a <see cref="ReadOnlySpan{T}"/> over a subset of the mip chain handles.
        /// </summary>
        /// <param name="index">Start index of the span.</param>
        /// <param name="length">Number of elements in the span.</param>
        /// <returns>A <see cref="ReadOnlySpan{TextureHandle}"/> representing the requested range of handles.</returns>
        public ReadOnlySpan<TextureHandle> AsSpan(int index, int length)
        {
            if (m_Handles == null || m_Handles.Length == 0)
            {
                return new ReadOnlySpan<TextureHandle>(Array.Empty<TextureHandle>());
            }

            index = Math.Clamp(index, 0, m_Handles.Length);
            length = Math.Clamp(length, 0, m_Handles.Length - index);

            return m_Handles.AsSpan(index, length);
        }

        /// <summary>
        /// Copies texture handles from a <see cref="ReadOnlySpan{TextureHandle}"/> into this mip chain.
        /// </summary>
        /// <param name="span">The span containing texture handles to copy.</param>
        public void CopyFrom(ReadOnlySpan<TextureHandle> span)
        {
            if (span == null || span.Length == 0)
            {
                m_Handles = Array.Empty<TextureHandle>();
                return;
            }

            if (m_Handles.Length != span.Length)
            {
                m_Handles = new TextureHandle[span.Length];
            }

            span.CopyTo(m_Handles);
        }

        /// <summary>
        /// Copies all texture handles from another <see cref="TextureHandleMipChain{Tdata}"/>.
        /// </summary>
        /// <param name="other">The other mip chain to copy from.</param>
        public void CopyFrom(TextureHandleMipChain<Tdata> other)
            => CopyFrom(other.m_Handles);

        /// <summary>
        /// Copies all texture handles from a given array.
        /// </summary>
        /// <param name="mipChain">Array of texture handles to copy.</param>
        public void CopyFrom(TextureHandle[] mipChain)
        {
            if (mipChain == null || mipChain.Length == 0)
            {
                m_Handles = Array.Empty<TextureHandle>();
                return;
            }

            if (m_Handles.Length != mipChain.Length)
            {
                m_Handles = new TextureHandle[mipChain.Length];
            }
            Array.Copy(mipChain, m_Handles, mipChain.Length);
        }

        /// <summary>
        /// Generates mip maps for the chain using the provided <see cref="GenerateMipMapsFunction"/> delegate.
        /// </summary>
        /// <param name="generateMipMapsFunc">Function that generates mip maps from a source to a destination handle for a given mip level.</param>
        public void GenerateMipMaps(GenerateMipMapsFunction generateMipMapsFunc)
        {
            if (m_Handles.Length <= 1)
            {
                return;
            }

            for (int i = 1; i < m_Handles.Length; ++i)
            {
                var src = m_Handles[i - 1];
                var dst = m_Handles[i];
                if (src.IsValid() && dst.IsValid())
                {
                    generateMipMapsFunc(src, dst, i);
                }
            }
        }

        /// <summary>
        /// Validates that the descriptor chain contains at least one mip level.
        /// </summary>
        /// <param name="descriptors">The descriptor chain to validate.</param>
        private static void ValidateMipChainDescriptors(RenderTextureDescriptorMipChain descriptors)
        {
            const string error = "MipCount must be greater than 0";
            Validate(descriptors.MipCount, (val) => { return val > 0; }, error);
        }

        /// <summary>
        /// Validates that the specified range (startMip, count) is within the bounds of the descriptor chain.
        /// </summary>
        /// <param name="descriptors">Descriptor chain to validate against.</param>
        /// <param name="startMip">Start index of the range.</param>
        /// <param name="count">Number of mip levels to include.</param>
        private static void ValidateMipCount(RenderTextureDescriptorMipChain descriptors, int startMip, int count)
        {
            string error = $"startMip and startMip + count must be in range {0} to {(descriptors.MipCount - 1)}";
            Validate(0, _ => startMip >= 0 && count > 0 && startMip + count <= descriptors.MipCount, error);
        }
    }
}