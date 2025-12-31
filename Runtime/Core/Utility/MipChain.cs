using Rayforge.RenderGraphExtensions.Rendering;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using static Rayforge.Utility.RuntimeCheck.Asserts;

namespace Rayforge.Utility.MipChain
{
    public static class MipChainHelpers
    {
        /// <summary>
        /// Default mip calculation: halve each dimension per level, clamped to 1.
        /// </summary>
        public static Vector2Int DefaultMipResolution(int mipLevel, Vector2Int baseRes)
        {
            int x = Math.Max(1, baseRes.x >> mipLevel);
            int y = Math.Max(1, baseRes.y >> mipLevel);
            return new Vector2Int(x, y);
        }
    }

    /// <summary>
    /// Represents the layout of a mip chain, independent of any rendering backend.
    /// Provides resolution per mip level based on a base resolution and a mip calculation function.
    /// </summary>
    public readonly struct MipChainLayout
    {
        /// <summary>
        /// Delegate to calculate the resolution of a given mip level from the base resolution.
        /// </summary>
        /// <param name="mipLevel">Mip level index (0 = full resolution).</param>
        /// <param name="baseRes">Base resolution (mip 0).</param>
        /// <returns>Resolution for the mip level.</returns>
        public delegate Vector2Int MipCreateFunc(int mipLevel, Vector2Int baseRes);

        private readonly MipCreateFunc m_MipFunc;
        private readonly int m_MipCount;
        private readonly Vector2Int m_BaseResolution;

        /// <summary>Mip resolution create function.</summary>
        public MipCreateFunc MipFunc => m_MipFunc;

        /// <summary>Number of mip levels in the chain.</summary>
        public int MipCount => m_MipCount;

        /// <summary>Base resolution (mip 0).</summary>
        public Vector2Int BaseResolution => m_BaseResolution;

        /// <summary>
        /// Creates a generic mip chain layout.
        /// </summary>
        /// <param name="baseResolution">Base resolution for mip 0.</param>
        /// <param name="mipCount">Number of mip levels.</param>
        /// <param name="mipFunc">Optional custom mip resolution function. Defaults to halving each dimension per mip.</param>
        public MipChainLayout(Vector2Int baseResolution, int mipCount, MipCreateFunc mipFunc = null)
        {
            if (baseResolution.x <= 0 || baseResolution.y <= 0)
                throw new ArgumentException("Base resolution must be greater than 0", nameof(baseResolution));

            if (mipCount <= 0)
                throw new ArgumentException("Mip count must be greater than 0", nameof(mipCount));

            m_BaseResolution = baseResolution;
            m_MipCount = mipCount;
            m_MipFunc = mipFunc ?? MipChainHelpers.DefaultMipResolution;
        }

        /// <summary>
        /// Returns the resolution for a given mip level.
        /// </summary>
        /// <param name="mipLevel">Mip level index.</param>
        /// <returns>Resolution for this mip level.</returns>
        public Vector2Int GetResolution(int mipLevel)
        {
            if (mipLevel < 0 || mipLevel >= m_MipCount)
                throw new ArgumentOutOfRangeException(nameof(mipLevel));

            return m_MipFunc(mipLevel, m_BaseResolution);
        }
    }

    /// <summary>
    /// Manages a chain of <see cref="RenderTextureDescriptor"/> instances for multiple mip levels.
    /// The resolution calculation is delegated to <see cref="MipChainLayout"/>, decoupling this class from RenderGraph.
    /// Supports dynamic resolution, mip count, and format changes.
    /// </summary>
    public sealed class DescriptorMipChain
    {
        private MipChainLayout m_Layout;
        private RenderTextureDescriptor[] m_Descriptors;
        private RenderTextureFormat m_Format;

        /// <summary>Read-only access to the mip level descriptors.</summary>
        public ReadOnlySpan<RenderTextureDescriptor> Descriptors => m_Descriptors;

        /// <summary>Access a specific mip level descriptor by index.</summary>
        /// <param name="index">The mip level index.</param>
        /// <returns>The <see cref="RenderTextureDescriptor"/> for the given mip level.</returns>
        public RenderTextureDescriptor this[int index] => m_Descriptors[index];

        /// <summary>The number of mip levels in this chain.</summary>
        public int MipCount => m_Layout.MipCount;

        /// <summary>The base resolution (mip 0) of the chain.</summary>
        public Vector2Int Resolution => m_Layout.BaseResolution;

        /// <summary>Width of the base resolution (mip 0).</summary>
        public int Width
        {
            get => m_Layout.BaseResolution.x;
            set => UpdateBaseResolution(new Vector2Int(value, m_Layout.BaseResolution.y));
        }

        /// <summary>Height of the base resolution (mip 0).</summary>
        public int Height
        {
            get => m_Layout.BaseResolution.y;
            set => UpdateBaseResolution(new Vector2Int(m_Layout.BaseResolution.x, value));
        }

        /// <summary>Format used for all descriptors in the chain.</summary>
        public RenderTextureFormat Format
        {
            get => m_Format;
            set => UpdateFormat(value);
        }

        /// <summary>
        /// Creates a new mip chain with the given base resolution, mip count, optional custom mip resolution function, and format.
        /// </summary>
        /// <param name="baseResolution">Base resolution (mip 0).</param>
        /// <param name="mipCount">Number of mip levels.</param>
        /// <param name="mipFunc">Optional custom mip resolution function.</param>
        /// <param name="format">Render texture format to use for all descriptors.</param>
        public DescriptorMipChain(Vector2Int baseResolution, int mipCount = 1, MipChainLayout.MipCreateFunc mipFunc = null, RenderTextureFormat format = RenderTextureFormat.Default)
        {
            m_Layout = new MipChainLayout(baseResolution, mipCount, mipFunc ?? MipChainHelpers.DefaultMipResolution);
            m_Format = format;
            m_Descriptors = new RenderTextureDescriptor[m_Layout.MipCount];
            InitDescriptors();
        }

        /// <summary>
        /// Initializes or refreshes all mip level descriptors based on the current layout and format.
        /// </summary>
        private void InitDescriptors()
        {
            for (int i = 0; i < m_Layout.MipCount; i++)
            {
                Vector2Int res = m_Layout.GetResolution(i);
                m_Descriptors[i] = new RenderTextureDescriptor(res.x, res.y, m_Format, 0);
            }
        }

        /// <summary>
        /// Updates the base resolution and recalculates all descriptors.
        /// </summary>
        /// <param name="newRes">New base resolution.</param>
        public void UpdateBaseResolution(Vector2Int newRes)
        {
            if (m_Layout.BaseResolution != newRes)
            {
                m_Layout = new MipChainLayout(newRes, m_Layout.MipCount, m_Layout.MipFunc);
                InitDescriptors();
            }
        }

        /// <summary>
        /// Updates the number of mip levels in the chain and refreshes all descriptors.
        /// </summary>
        /// <param name="newMipCount">New mip count.</param>
        public void UpdateMipCount(int newMipCount)
        {
            if (m_Layout.MipCount != newMipCount)
            {
                m_Layout = new MipChainLayout(m_Layout.BaseResolution, newMipCount, m_Layout.MipFunc);
                Array.Resize(ref m_Descriptors, newMipCount);
                InitDescriptors();
            }
        }

        /// <summary>
        /// Updates the render texture format for all descriptors in the chain.
        /// </summary>
        /// <param name="newFormat">New <see cref="RenderTextureFormat"/>.</param>
        private void UpdateFormat(RenderTextureFormat newFormat)
        {
            if (m_Format != newFormat)
            {
                m_Format = newFormat;
                for (int i = 0; i < m_Descriptors.Length; i++)
                    m_Descriptors[i].colorFormat = m_Format;
            }
        }
    }

    /// <summary>
    /// Represents a chain of handles corresponding to mip levels of a texture.
    /// Provides creation, resizing, copying, and optional generation of successive mip levels.
    /// </summary>
    /// <typeparam name="Thandle">Type of the handle (e.g., TextureHandle, RenderTexture, etc.).</typeparam>
    /// <typeparam name="Tdata">Optional user data passed to the creation function for context or parameters.</typeparam>
    public class MipChain<Thandle, Tdata> 
        where Tdata : class
    {
        /// <summary>
        /// Delegate for creating a handle for a mip level.
        /// </summary>
        /// <param name="handle">Reference to the handle to create.</param>
        /// <param name="descriptor">Descriptor describing the texture to create.</param>
        /// <param name="mipLevel">Index of the mip level being created.</param>
        /// <param name="data">Optional user data.</param>
        /// <returns>True if creation was successful; otherwise false.</returns>
        public delegate bool CreateFunction(ref Thandle handle, RenderTextureDescriptor descriptor, int mipLevel, Tdata data = null);

        /// <summary>
        /// Delegate for generating mip maps between two handles.
        /// </summary>
        /// <param name="src">Source handle (previous level).</param>
        /// <param name="dest">Destination handle (current level).</param>
        /// <param name="mipLevel">The mip level index being generated.</param>
        public delegate void GenerateFunction(Thandle src, Thandle dest, int mipLevel);

        private Thandle[] m_Handles;
        private CreateFunction m_CreateFunc;

        /// <summary>Read-only access to the handles.</summary>
        public ReadOnlySpan<Thandle> Handles => m_Handles;

        /// <summary>Access a specific mip level handle by index.</summary>
        /// <param name="index">The mip level index.</param>
        public Thandle this[int index] => m_Handles[index];

        /// <summary>Total number of mip levels.</summary>
        public int MipCount => m_Handles?.Length ?? 0;

        /// <summary>
        /// Initializes the mip chain with a handle creation function.
        /// </summary>
        /// <param name="createFunc">Function to create each mip level.</param>
        public MipChain(CreateFunction createFunc)
        {
            if (createFunc == null) throw new ArgumentNullException(nameof(createFunc));
            m_CreateFunc = createFunc;
            m_Handles = Array.Empty<Thandle>();
        }

        /// <summary>
        /// Creates all mip levels from a <see cref="DescriptorMipChain"/>.
        /// </summary>
        /// <param name="descriptorChain">The descriptor chain containing mip resolutions.</param>
        /// <param name="data">Optional user data.</param>
        /// <returns>True if all mip levels were successfully created.</returns>
        public bool Create(DescriptorMipChain descriptorChain, Tdata data = null)
            => Create(descriptorChain, 0, descriptorChain.MipCount, data);

        /// <summary>
        /// Creates only the first mip level from a <see cref="DescriptorMipChain"/>.
        /// </summary>
        /// <param name="descriptorChain">The descriptor chain.</param>
        /// <param name="data">Optional user data.</param>
        /// <returns>True if creation succeeded.</returns>
        public bool CreateFirst(DescriptorMipChain descriptorChain, Tdata data = null)
            => Create(descriptorChain, 0, 1, data);

        /// <summary>
        /// Creates a range of mip levels from a <see cref="DescriptorMipChain"/>, starting from <paramref name="startMip"/> and creating <paramref name="count"/> levels.
        /// </summary>
        /// <param name="descriptorChain">Descriptor chain containing the mip resolutions.</param>
        /// <param name="startMip">Index of the first mip to create.</param>
        /// <param name="count">Number of mip levels to create.</param>
        /// <param name="data">Optional user data.</param>
        /// <returns>True if all mip levels in the range were successfully created.</returns>
        public bool Create(DescriptorMipChain descriptorChain, int startMip, int count, Tdata data = null)
        {
            if (descriptorChain == null || descriptorChain.MipCount == 0)
                throw new ArgumentException("DescriptorMipChain must not be null or empty.", nameof(descriptorChain));

            var descriptors = descriptorChain.Descriptors;

            startMip = Mathf.Clamp(startMip, 0, descriptors.Length - 1);
            count = Mathf.Clamp(count, 1, descriptors.Length - startMip);

            if (m_Handles.Length != descriptors.Length)
                Resize(descriptors.Length);

            for (int i = startMip; i < startMip + count; i++)
            {
                if (!Create(i, descriptors[i], data))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Creates all mip levels from a <see cref="DescriptorMipChain"/>.
        /// </summary>
        /// <param name="descriptorChain">The descriptor chain containing mip resolutions.</param>
        /// <param name="data">Optional user data.</param>
        /// <returns>True if all mip levels were successfully created.</returns>
        public bool Create(int width, int height, RenderTextureDescriptor descriptor, int mipCount, Tdata data = null)
            => Create(width, height, descriptor, 0, mipCount, data);

        /// <summary>
        /// Creates only the first mip level from a <see cref="DescriptorMipChain"/>.
        /// </summary>
        /// <param name="descriptorChain">The descriptor chain.</param>
        /// <param name="data">Optional user data.</param>
        /// <returns>True if creation succeeded.</returns>
        public bool CreateFirst(int width, int height, RenderTextureDescriptor descriptor, Tdata data = null)
            => Create(width, height, descriptor, 0, 1, data);

        /// <summary>
        /// Creates a range of mip levels from a <see cref="DescriptorMipChain"/>, starting from <paramref name="startMip"/> and creating <paramref name="count"/> levels.
        /// </summary>
        /// <param name="descriptorChain">Descriptor chain containing the mip resolutions.</param>
        /// <param name="startMip">Index of the first mip to create.</param>
        /// <param name="count">Number of mip levels to create.</param>
        /// <param name="data">Optional user data.</param>
        /// <returns>True if all mip levels in the range were successfully created.</returns>
        public bool Create(int width, int height, RenderTextureDescriptor descriptor, int startMip, int count, Tdata data = null)
        {
            if (m_Handles.Length != count)
                Resize(count);

            Vector2Int baseRes = new Vector2Int(width, height);
            for (int i = startMip; i < startMip + count; i++)
            {
                var mipRes = MipChainHelpers.DefaultMipResolution(i, baseRes);
                descriptor.width = mipRes.x;
                descriptor.height = mipRes.y;

                if (!Create(i, descriptor, data))
                    return false;
            }

            return true;
        }

        private bool Create(int index, RenderTextureDescriptor descriptor, Tdata data = null)
            => m_CreateFunc(ref m_Handles[index], descriptor, index, data);

        /// <summary>
        /// Resizes the internal array to <paramref name="newLength"/>.
        /// </summary>
        /// <param name="newLength">New array length.</param>
        public void Resize(int newLength)
            => Resize(newLength, 0, MipCount);

        /// <summary>
        /// Resizes the array and optionally preserves a subset of existing elements.
        /// </summary>
        /// <param name="newLength">New array length.</param>
        /// <param name="preserveIndex">Start index in the old array to preserve.</param>
        /// <param name="preserveCount">Number of elements to preserve.</param>
        public void Resize(int newLength, int preserveIndex, int preserveCount)
        {
            if (newLength < 0) newLength = 0;
            if (MipCount == newLength) return;
            if (newLength == 0) { m_Handles = Array.Empty<Thandle>(); return; }

            var newHandles = new Thandle[newLength];

            if (m_Handles != null && preserveCount > 0)
            {
                preserveIndex = Math.Clamp(preserveIndex, 0, m_Handles.Length - 1);
                preserveCount = Math.Min(preserveCount, m_Handles.Length - preserveIndex);
                preserveCount = Math.Min(preserveCount, newHandles.Length);

                Array.Copy(m_Handles, preserveIndex, newHandles, 0, preserveCount);
            }

            m_Handles = newHandles;
        }

        /// <summary>
        /// Returns a read-only span of handles.
        /// </summary>
        /// <param name="start">Start index of the span.</param>
        /// <param name="length">Number of elements in the span.</param>
        public ReadOnlySpan<Thandle> AsSpan(int start, int length)
        {
            start = Math.Clamp(start, 0, MipCount);
            length = Math.Clamp(length, 0, MipCount - start);
            return m_Handles.AsSpan(start, length);
        }

        /// <summary>
        /// Copies all handles from another mip chain.
        /// </summary>
        /// <param name="other">Source mip chain.</param>
        public void CopyFrom(MipChain<Thandle, Tdata> other)
        {
            if (other == null) 
                throw new ArgumentNullException(nameof(other));

            Resize(other.MipCount);
            for (int i = 0; i < other.MipCount; i++)
                m_Handles[i] = other[i];
        }

        /// <summary>
        /// Copies a range of handles from another mip chain.
        /// </summary>
        /// <param name="other">Source mip chain.</param>
        /// <param name="start">Start index in the source chain.</param>
        /// <param name="count">Number of handles to copy.</param>
        public void CopyFrom(MipChain<Thandle, Tdata> other, int start, int count)
        {
            if (other == null) 
                throw new ArgumentNullException(nameof(other));

            start = Math.Clamp(start, 0, other.MipCount);
            count = Math.Clamp(count, 0, other.MipCount - start);

            Resize(count);
            for (int i = 0; i < count; i++)
                m_Handles[i] = other[start + i];
        }

        /// <summary>
        /// Generates mip maps for the chain using the provided delegate.
        /// </summary>
        /// <param name="generateFunc">Function that generates a mip from source to destination.</param>
        public void GenerateMipMaps(GenerateFunction generateFunc)
        {
            if (MipCount <= 1 || generateFunc == null) return;
            for (int i = 1; i < MipCount; i++)
            {
                generateFunc(m_Handles[i - 1], m_Handles[i], i);
            }
        }
    }
}