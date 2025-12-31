using System;
using System.Collections.Generic;
using UnityEngine;

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
        public IReadOnlyList<RenderTextureDescriptor> Descriptors => m_Descriptors;

        /// <summary>Access a specific mip level descriptor by index.</summary>
        /// <param name="index">The mip level index.</param>
        /// <returns>The <see cref="RenderTextureDescriptor"/> for the given mip level.</returns>
        public RenderTextureDescriptor this[int index] => m_Descriptors[index];

        /// <summary>The number of mip levels in this chain.</summary>
        public int MipCount
        {
            get => m_Layout.MipCount;
            set => UpdateMipCount(value);
        }

        /// <summary>The base resolution (mip 0) of the chain.</summary>
        public Vector2Int Resolution
        {
            get => m_Layout.BaseResolution;
            set => UpdateBaseResolution(value);
        }

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
        /// <param name="width">Base resolution (mip 0) in x dimension.</param>
        /// <param name="height">Base resolution (mip 0) in y dimension.</param>
        /// <param name="mipCount">Number of mip levels.</param>
        /// <param name="mipFunc">Optional custom mip resolution function.</param>
        /// <param name="format">Render texture format to use for all descriptors.</param>
        public DescriptorMipChain(int width, int height, int mipCount = 1, MipChainLayout.MipCreateFunc mipFunc = null, RenderTextureFormat format = RenderTextureFormat.Default)
            : this(new MipChainLayout(new Vector2Int(width, height), mipCount, mipFunc ?? MipChainHelpers.DefaultMipResolution))
        { }

        /// <summary>
        /// Creates a new mip chain with the given base resolution, mip count, optional custom mip resolution function, and format.
        /// </summary>
        /// <param name="baseResolution">Base resolution (mip 0).</param>
        /// <param name="mipCount">Number of mip levels.</param>
        /// <param name="mipFunc">Optional custom mip resolution function.</param>
        /// <param name="format">Render texture format to use for all descriptors.</param>
        public DescriptorMipChain(Vector2Int baseResolution, int mipCount = 1, MipChainLayout.MipCreateFunc mipFunc = null, RenderTextureFormat format = RenderTextureFormat.Default)
            : this(new MipChainLayout(baseResolution, mipCount, mipFunc ?? MipChainHelpers.DefaultMipResolution))
        { }

        /// <summary>
        /// Creates a new mip chain with the given base resolution, mip count, optional custom mip resolution function, and format.
        /// </summary>
        /// <param name="mipChainLayout"><see cref="MipChainLayout"/> defining the mip chain.</param>
        /// <param name="format">Render texture format to use for all descriptors.</param>
        public DescriptorMipChain(MipChainLayout mipChainLayout, RenderTextureFormat format = RenderTextureFormat.Default)
        {
            m_Layout = mipChainLayout;
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
    {
        /// <summary>
        /// Delegate for creating a handle for a mip level.
        /// </summary>
        /// <param name="descriptor">Descriptor describing the texture to create.</param>
        /// <param name="mipLevel">Index of the mip level being created.</param>
        /// <param name="data">Optional user data.</param>
        /// <returns>True if creation was successful; otherwise false.</returns>
        public delegate Thandle CreateFunction(RenderTextureDescriptor descriptor, int mipLevel, Tdata data = default);

        /// <summary>
        /// Delegate for generating mip maps between two handles.
        /// </summary>
        /// <param name="src">Source handle (previous level).</param>
        /// <param name="dest">Destination handle (current level).</param>
        /// <param name="mipLevel">The mip level index being generated.</param>
        public delegate void GenerateFunction(Thandle src, Thandle dest, int mipLevel);

        protected Thandle[] m_Handles;
        private CreateFunction m_CreateFunc;

        /// <summary>Read-only access to the handles.</summary>
        public IReadOnlyList<Thandle> Handles => m_Handles ?? Array.Empty<Thandle>();

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
            if (createFunc == null)
                throw new ArgumentNullException(nameof(createFunc));

            m_CreateFunc = createFunc;
            m_Handles = Array.Empty<Thandle>();
        }

        /// <summary>
        /// Creates all mip levels from the specified <see cref="DescriptorMipChain"/>.
        /// Handles are stored at indices starting from 0 in the handle array.
        /// The handle array is resized to exactly match the number of mip levels in the chain.
        /// </summary>
        /// <param name="descriptorChain">The descriptor chain providing descriptors for each mip level.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void Create(DescriptorMipChain descriptorChain, Tdata data = default)
        {
            if (descriptorChain == null || descriptorChain.MipCount == 0)
                throw new ArgumentException("DescriptorMipChain must not be null or empty.", nameof(descriptorChain));

            var descriptors = descriptorChain.Descriptors;

            Resize(descriptors.Count);
            for (int i = 0; i < descriptors.Count; i++)
                Create(i, descriptors[i], data);
        }

        /// <summary>
        /// Creates all mip levels based on a single <see cref="RenderTextureDescriptor"/> as the base descriptor.
        /// Handles are stored at indices starting from 0 in the handle array.
        /// The handle array is resized to exactly match the number of mip levels being created. 
        /// If it was previously larger or smaller, it will be resized to <paramref name="mipCount"/>.
        /// </summary>
        /// <param name="width">Width of the base mip level.</param>
        /// <param name="height">Height of the base mip level.</param>
        /// <param name="descriptor">Base descriptor for mip creation; will be resized for each mip level.</param>
        /// <param name="mipCount">Total number of mip levels to create.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void Create(int width, int height, RenderTextureDescriptor descriptor, int mipCount = 1, Tdata data = default)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Base width and height must be greater than zero.");

            if (mipCount <= 0)
                throw new ArgumentException("Count must be greater than zero.");

            Vector2Int baseRes = new Vector2Int(width, height);

            Resize(mipCount);
            for (int i = 0; i < mipCount; i++)
            {
                var mipRes = MipChainHelpers.DefaultMipResolution(i, baseRes);
                descriptor.width = mipRes.x;
                descriptor.height = mipRes.y;

                Create(i, descriptor, data);
            }
        }

        /// <summary>
        /// Internal method that invokes the creation delegate for a single mip level.
        /// </summary>
        /// <param name="index">Index of the mip level to create.</param>
        /// <param name="descriptor">Descriptor to use for this mip level.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        protected void Create(int index, RenderTextureDescriptor descriptor, Tdata data = default)
            => m_Handles[index] = m_CreateFunc.Invoke(descriptor, index, data);

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
            if (newLength == 0)
            {
                m_Handles = Array.Empty<Thandle>();
                return;
            }

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
            => CopyFrom(other, 0, other.MipCount);

        /// <summary>
        /// Copies a range of handles from another mip chain.
        /// <para>
        /// This method can bypass the usual safety guarantees of a mip chain
        /// (for example, contiguous layout or complete mip coverage) and is
        /// intended for advanced usage where such constraints are managed manually.
        /// </para>
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
        /// Creates a MipChain from a single handle. The chain will have length 1.
        /// Useful when no actual mip levels are needed and a single texture/handle represents the entire chain.
        /// </summary>
        /// <param name="handle">The single handle representing the chain.</param>
        public void CopyFrom(Thandle handle)
        {
            Resize(1);
            m_Handles[0] = handle;
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

    /// <summary>
    /// Represents an "unsafe" mip chain with additional flexibility for advanced scenarios.
    /// This class extends <see cref="MipChain{Thandle, Tdata}"/> and allows:
    /// - Creating mip levels starting at arbitrary indices in the handle array.
    /// - Optionally shrinking the handle array to exactly fit the created mip levels.
    /// - Stacking multiple mip chains into a single handle array.
    /// 
    /// Use with caution: manipulating start indices and shrink behavior can break
    /// the assumptions of a standard mip chain, so this class is intended for advanced usage
    /// where you explicitly want to bypass the safety guarantees of <see cref="MipChain{Thandle, Tdata}"/>.
    /// </summary>
    /// <typeparam name="Thandle">Type of the handle (e.g., TextureHandle, RenderTexture, etc.).</typeparam>
    /// <typeparam name="Tdata">Optional user data passed to the creation function for context or parameters.</typeparam>
    public class UnsafeMipChain<Thandle, Tdata> : MipChain<Thandle, Tdata>
    {
        /// <summary>
        /// Initializes the mip chain with a handle creation function.
        /// </summary>
        /// <param name="createFunc">Function to create each mip level.</param>
        public UnsafeMipChain(CreateFunction createFunc)
            : base(createFunc)
        { }

        /// <summary>
        /// Creates only the first mip level from the specified <see cref="DescriptorMipChain"/>.
        /// Handles are stored at index 0 in the handle array.
        /// The handle array is only enlarged if necessary; it will not be shrunk,
        /// so existing handles in the array are preserved.
        /// </summary>
        /// <param name="descriptorChain">The descriptor chain providing the descriptor for the first mip level.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void CreateFirst(DescriptorMipChain descriptorChain, Tdata data = default)
            => CreateUnsafe(descriptorChain, 0, 1, 0, false, data);

        /// <summary>
        /// Creates a range of mip levels from the specified <see cref="DescriptorMipChain"/>.
        /// Handles are stored at the same indices as their corresponding descriptors.
        /// The handle array is only enlarged if necessary; it will never be shrunk.
        /// </summary>
        /// <param name="descriptorChain">The descriptor chain providing descriptors for the mip levels.</param>
        /// <param name="startMip">Index of the first mip level to create.</param>
        /// <param name="count">Number of mip levels to create.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void CreateUnsafe(DescriptorMipChain descriptorChain, int startMip, int count, Tdata data = default)
            => CreateUnsafe(descriptorChain, startMip, count, startMip, false, data);

        /// <summary>
        /// Creates a range of mip levels from the specified <see cref="DescriptorMipChain"/>
        /// and stores the resulting handles at a specified start index in the handle array.
        /// This allows stacking multiple mip chains into a single handle array.
        /// Shrink behavior can be controlled.
        /// </summary>
        /// <param name="descriptorChain">The descriptor chain providing descriptors for the mip levels.</param>
        /// <param name="startMip">Index of the first mip level to create from the descriptor chain.</param>
        /// <param name="count">Number of mip levels to create.</param>
        /// <param name="handleStartIndex">The start index in the handle array where the first handle will be stored.</param>
        /// <param name="shrink">
        /// If true, allows the handle array to be resized down if it is larger than needed; 
        /// otherwise, the array is only enlarged.
        /// </param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void CreateUnsafe(DescriptorMipChain descriptorChain, int startMip, int count, int handleStartIndex, bool shrink = false, Tdata data = default)
        {
            if (descriptorChain == null || descriptorChain.MipCount == 0)
                throw new ArgumentException("DescriptorMipChain must not be null or empty.", nameof(descriptorChain));

            var descriptors = descriptorChain.Descriptors;

            startMip = Mathf.Clamp(startMip, 0, descriptors.Count - 1);
            count = Mathf.Clamp(count, 1, descriptors.Count - startMip);

            if (m_Handles.Length < handleStartIndex + count || shrink)
                Resize(handleStartIndex + count);

            for (int i = 0; i < count; i++)
                Create(handleStartIndex + i, descriptors[startMip + i], data);
        }

        /// <summary>
        /// Creates a range of mip levels starting from <paramref name="startMip"/>.
        /// Handles are stored at indices starting from <paramref name="startMip"/> in the handle array.
        /// The handle array is only enlarged if necessary; it will never be shrunk in this overload. 
        /// </summary>
        /// <param name="width">Width of the base mip level.</param>
        /// <param name="height">Height of the base mip level.</param>
        /// <param name="descriptor">Base descriptor for mip creation; will be resized for each mip level.</param>
        /// <param name="startMip">Index of the first mip level to create.</param>
        /// <param name="count">Number of mip levels to create starting from <paramref name="startMip"/>.</param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void CreateUnsafe(int width, int height, RenderTextureDescriptor descriptor, int startMip, int count, Tdata data = default)
            => CreateUnsafe(width, height, descriptor, startMip, count, startMip, false, data);

        /// <summary>
        /// Creates a range of mip levels with full control over the handle array.
        /// Handles are stored starting at <paramref name="handleStartIndex"/> in the handle array.
        /// If <paramref name="shrink"/> is true, the handle array may be reduced to exactly fit the created handles. 
        /// If false, the array will only be enlarged if necessary.
        /// This overload is useful for stacking multiple mip chains into a single handle array or for special handle layouts.
        /// </summary>
        /// <param name="width">Width of the base mip level.</param>
        /// <param name="height">Height of the base mip level.</param>
        /// <param name="descriptor">Base descriptor for mip creation; will be resized for each mip level.</param>
        /// <param name="startMip">Index of the first mip level to create.</param>
        /// <param name="count">Number of mip levels to create starting from <paramref name="startMip"/>.</param>
        /// <param name="handleStartIndex">Start index in the handle array where the first handle will be stored.</param>
        /// <param name="shrink">
        /// If true, allows the handle array to be resized down if it is larger than needed; otherwise only enlarges.
        /// </param>
        /// <param name="data">Optional user data passed to the creation function.</param>
        public void CreateUnsafe(int width, int height, RenderTextureDescriptor descriptor, int startMip, int count, int handleStartIndex, bool shrink = false, Tdata data = default)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Base width and height must be greater than zero.");

            if (count <= 0)
                throw new ArgumentException("Count must be greater than zero.");

            Vector2Int baseRes = new Vector2Int(width, height);

            if (m_Handles.Length < handleStartIndex + count || shrink)
                Resize(handleStartIndex + count);

            for (int i = 0; i < count; i++)
            {
                var mipRes = MipChainHelpers.DefaultMipResolution(startMip + i, baseRes);
                descriptor.width = mipRes.x;
                descriptor.height = mipRes.y;

                Create(handleStartIndex + i, descriptor, data);
            }
        }

        /// <summary>
        /// Sets a handle at an arbitrary index in the mip chain.
        /// 
        /// This method bypasses all mip chain consistency guarantees:
        /// - The index does not need to correspond to a valid mip level.
        /// - The handle array may grow and contain gaps.
        /// - No validation of mip ordering or resolution is performed.
        /// 
        /// Intended for advanced scenarios where mip chain invariants
        /// are managed externally by the caller.
        /// </summary>
        /// <param name="index">Target index in the handle array.</param>
        /// <param name="handle">Handle to assign.</param>
        public void SetHandleUnsafe(int index, Thandle handle)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            var expectedSize = index + 1;
            if (m_Handles.Length < expectedSize)
                Resize(expectedSize);

            m_Handles[index] = handle;
        }

        /// <summary>
        /// Copies a range of handles from another mip chain.
        /// <para>
        /// This method can bypass the usual safety guarantees of a mip chain
        /// (for example, contiguous layout or complete mip coverage) and is
        /// intended for advanced usage where such constraints are managed manually.
        /// </para>
        /// </summary>
        /// <param name="other">Source mip chain.</param>
        /// <param name="start">Start index in the source chain.</param>
        /// <param name="count">Number of handles to copy.</param>
        /// <param name="handleStartIndex">Start index in the handle array where the first handle will be stored.</param>
        public void CopyFromUnsafe(MipChain<Thandle, Tdata> other, int start, int count, int handleStartIndex)
            => CopyFromUnsafe(other.Handles, start, count, handleStartIndex);

        /// <summary>
        /// Copies a range of handles from another mip chain.
        /// <para>
        /// This method can bypass the usual safety guarantees of a mip chain
        /// (for example, contiguous layout or complete mip coverage) and is
        /// intended for advanced usage where such constraints are managed manually.
        /// </para>
        /// </summary>
        /// <param name="other">Source mip chain.</param>
        /// <param name="start">Start index in the source chain.</param>
        /// <param name="count">Number of handles to copy.</param>
        /// <param name="handleStartIndex">Start index in the handle array where the first handle will be stored.</param>
        public void CopyFromUnsafe(IReadOnlyList<Thandle> other, int start, int count, int handleStartIndex)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (handleStartIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(handleStartIndex));

            if (other.Count == 0 || count <= 0)
                return;

            start = Math.Clamp(start, 0, other.Count);
            count = Math.Clamp(count, 0, other.Count - start);

            var requiredSize = handleStartIndex + count;
            if (m_Handles.Length < requiredSize)
                Resize(requiredSize);

            for (int i = 0; i < count; i++)
                m_Handles[handleStartIndex + i] = other[start + i];
        }
    }
}