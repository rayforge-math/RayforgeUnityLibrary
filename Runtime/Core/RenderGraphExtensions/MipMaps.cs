using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.UIElements;
using static Rayforge.Utility.RuntimeCheck.Asserts;

namespace Rayforge.RenderGraphExtensions.Rendering
{
    /// <summary>
    /// Delegate used to generate the resolution for a mip level from a base resolution within a RenderGraph context.
    /// Typically used to create a chain of <see cref="TextureHandle"/> descriptors for custom mip chains.
    /// </summary>
    /// <param name="mipLevel">The mip level index (0 is full resolution).</param>
    /// <param name="baseRes">The base resolution (mip 0).</param>
    /// <returns>The resolution for the given mip level.</returns>
    public delegate Vector2Int MipCreateFunc(int mipLevel, Vector2Int baseRes);

    /// <summary>
    /// Manages a chain of <see cref="RenderTextureDescriptor"/> instances for multiple mip levels,
    /// specifically designed for use with RenderGraph passes and <see cref="TextureHandle"/> allocations.
    /// Handles automatic resolution scaling per mip and consistent format updates across the chain.
    /// </summary>
    public struct RenderTextureDescriptorMipChain
    {
        /// <summary>Function used to generate mip resolutions for each level in the RenderGraph context.</summary>
        private readonly MipCreateFunc k_MipCreateFunc;

        /// <summary>Array of descriptors for each mip level, to be used for TextureHandle creation.</summary>
        private RenderTextureDescriptor[] m_Descriptors;

        /// <summary>Read-only access to the mip level descriptors.</summary>
        public IReadOnlyList<RenderTextureDescriptor> Descriptors => m_Descriptors;

        /// <summary>Access a specific mip level descriptor by index.</summary>
        /// <param name="index">The mip level index.</param>
        /// <returns>The <see cref="RenderTextureDescriptor"/> for the given mip, suitable for RenderGraph texture creation.</returns>
        public RenderTextureDescriptor this[int index] => m_Descriptors[index];

        private RenderTextureFormat m_Format;
        /// <summary>Format of all descriptors in the chain.</summary>
        public RenderTextureFormat Format
        {
            get => m_Format;
            set => UpdateFormat(value);
        }

        public int Length => m_Descriptors?.Length ?? 0;
        /// <summary>Total number of mip levels in the chain, used to allocate corresponding TextureHandles in RenderGraph.</summary>
        public int MipCount
        {
            get => Length;
            set => UpdateMipCount(value);
        }

        private Vector2Int m_Resolution;
        /// <summary>Width and height of the base resolution (mip 0), used as reference for all mip levels.</summary>
        public Vector2Int Resolution
        {
            get => m_Resolution;
            set => UpdateResolution(value);
        }

        /// <summary>Width of the base resolution (mip 0).</summary>
        public int Width
        {
            get => m_Resolution.x;
            set => Resolution = new Vector2Int { x = value, y = m_Resolution.y };
        }

        /// <summary>Height of the base resolution (mip 0).</summary>
        public int Height
        {
            get => m_Resolution.y;
            set => Resolution = new Vector2Int { x = m_Resolution.x, y = value };
        }

        /// <summary>
        /// Creates a mip chain for RenderGraph usage with the given resolution, optional custom mip generation function, mip count, and format.
        /// Each descriptor can be used to allocate a <see cref="TextureHandle"/> for a RenderGraph pass.
        /// </summary>
        /// <param name="width">Base width of the mip 0 level.</param>
        /// <param name="height">Base height of the mip 0 level.</param>
        /// <param name="createFunc">Optional custom mip resolution function.</param>
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
        /// Updates the base resolution of the mip chain and recalculates all mip level descriptors.
        /// Intended for use before allocating <see cref="TextureHandle"/>s in RenderGraph.
        /// </summary>
        /// <param name="resolution">The new base resolution.</param>
        private void UpdateResolution(Vector2Int resolution)
        {
            if (!Resolution.Equals(resolution))
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
        /// Updates the number of mip levels and reinitializes descriptors.
        /// Relevant for allocating a variable number of TextureHandles in RenderGraph.
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
        /// Updates the format of all descriptors.
        /// Ensures consistency when creating TextureHandles for RenderGraph passes.
        /// </summary>
        /// <param name="format">The new <see cref="RenderTextureFormat"/> to use.</param>
        private void UpdateFormat(RenderTextureFormat format)
        {
            if (m_Format != format)
            {
                m_Format = format;
                for (int i = 0; i < m_Descriptors.Length; ++i)
                {
                    m_Descriptors[i].colorFormat = format;
                }
            }
        }

        /// <summary>
        /// Initializes the mip chain descriptors for use with RenderGraph texture allocations.
        /// </summary>
        /// <param name="mipCount">Number of mip levels.</param>
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
        /// Default mip creation function for RenderGraph: halves the resolution per mip level, clamped to 1.
        /// </summary>
        /// <param name="mipLevel">The mip level index.</param>
        /// <param name="resolution">The base resolution (mip 0).</param>
        /// <returns>The resolution for the mip level.</returns>
        public static Vector2Int DefaultMipCreate(int mipLevel, Vector2Int resolution)
        {
            return new Vector2Int { x = DefaultMipCreate(mipLevel, resolution.x), y = DefaultMipCreate(mipLevel, resolution.y) };
        }

        /// <summary>
        /// Default mip creation for a single dimension.
        /// </summary>
        /// <param name="mipLevel">The mip level index.</param>
        /// <param name="resolution">The base resolution for this dimension.</param>
        /// <returns>Halved resolution for the mip level, minimum 1.</returns>
        private static int DefaultMipCreate(int mipLevel, int resolution)
        {
            return Mathf.Max(1, resolution >> mipLevel);
        }

        /// <summary>
        /// Validates that the mip count is greater than zero.
        /// </summary>
        /// <param name="mipCount">Mip count to validate.</param>
        private static void ValidateMipCount(int mipCount)
        {
            const string error = "MipCount must be greater than 0";
            Validate(mipCount, (val) => val > 0, error);
        }

        /// <summary>
        /// Validates that the resolution has positive width and height.
        /// </summary>
        /// <param name="resolution">Resolution to validate.</param>
        private static void ValidateResolution(Vector2Int resolution)
        {
            const string error = "Resolution must be greater than 0";
            Validate(resolution, (val) => resolution.x > 0 && resolution.y > 0, error);
        }
    }

    /// <summary>
    /// Represents a chain of <see cref="TextureHandle"/>s corresponding to mip levels of a texture
    /// specifically for use in RenderGraph passes. 
    /// 
    /// Unity's standard RenderTexture MipChain can be cumbersome in RenderGraph because:
    /// - Each mip level needs its own <see cref="TextureHandle"/> allocation.
    /// - Copying or generating mips between levels requires explicit pass setup.
    /// - Automatic mip generation via standard RenderTexture is not directly supported in RenderGraph.
    /// 
    /// This structure simplifies the process by:
    /// - Creating all mip levels via a user-provided function.
    /// - Allowing optional mip map generation between handles in a RenderGraph-friendly way.
    /// - Providing easy access to individual mip handles and read-only spans for pass binding.
    /// </summary>
    /// <typeparam name="Tdata">
    /// Optional user data passed to the texture creation function, useful for passing context
    /// or resources needed during RenderGraph allocation.
    /// </typeparam>
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
                Resize(descriptors.MipCount);
                /*
                var oldHandles = m_Handles;
                m_Handles = new TextureHandle[descriptors.Length];
                if (oldHandles != null && startMip > 0)
                    Array.Copy(oldHandles, m_Handles, Math.Min(startMip, oldHandles.Length));
                */
            }

            for (int i = startMip; i < endMip; ++i)
            {
                if (!Create(i, descriptors[i], data))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Resizes the internal handle array to the specified new length.
        /// Preserves as many existing handles as possible up to the new length.
        /// </summary>
        /// <param name="newLength">The new desired array length. Must be ≥ 0.</param>
        public void Resize(int newLength)
            => Resize(newLength, 0, MipCount);

        /// <summary>
        /// Resizes the internal handle array to the specified new length.
        /// Preserves a subset of existing handles starting at <paramref name="preserveIndex"/>
        /// up to <paramref name="preserveCount"/> elements. All other slots are left uninitialized (default).
        /// </summary>
        /// <param name="newLength">The new desired array length. Must be ≥ 0.</param>
        /// <param name="preserveIndex">
        /// The start index in the old array from which to copy handles into the new array.
        /// </param>
        /// <param name="preserveCount">
        /// The number of elements to preserve starting from <paramref name="preserveIndex"/>.
        /// Clamped automatically to the available range.
        /// </param>
        public void Resize(int newLength, int preserveIndex, int preserveCount)
        {
            if (newLength < 0)
                newLength = 0;

            if (MipCount == newLength)
                return;

            if (newLength == 0)
            {
                m_Handles = Array.Empty<TextureHandle>();
                return;
            }

            var newHandles = new TextureHandle[newLength];

            if (m_Handles != null && preserveCount > 0)
            {
                preserveIndex = Math.Max(0, Math.Min(preserveIndex, m_Handles.Length - 1));
                preserveCount = Math.Min(preserveCount, m_Handles.Length - preserveIndex);
                preserveCount = Math.Min(preserveCount, newHandles.Length);

                if (preserveCount > 0)
                    Array.Copy(m_Handles, preserveIndex, newHandles, 0, preserveCount);
            }

            m_Handles = newHandles;
        }

        /// <summary>
        /// Imports a <see cref="TextureHandle"/> into the mip chain at the specified index.
        /// </summary>
        /// <param name="mipLevel">The mip level index where the handle should be stored.</param>
        /// <param name="handle">The <see cref="TextureHandle"/> to import.</param>
        /// <returns>
        /// <c>true</c> if the handle was successfully imported; 
        /// <c>false</c> if the index is out of range or the handle is invalid.
        /// </returns>
        public bool SetMipHandle(int mipLevel, TextureHandle handle)
        {
            if (mipLevel < 0 || mipLevel >= MipCount)
                throw new IndexOutOfRangeException(
                    $"Mip level {mipLevel} is out of range (0 to {MipCount - 1}).");

            if (!handle.IsValid())
                throw new ArgumentException(
                    $"TextureHandle for mip {mipLevel} is invalid.", nameof(handle));

            m_Handles[mipLevel] = handle;
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