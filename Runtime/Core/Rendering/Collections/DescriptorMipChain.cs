using System;
using System.Collections.Generic;
using UnityEngine;

using Rayforge.Rendering.Collections.Helpers;

namespace Rayforge.Rendering.Collections
{
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

        /// <summary>
        /// Returns a read-only span of descriptors.
        /// </summary>
        public ReadOnlySpan<RenderTextureDescriptor> AsSpan()
            => m_Descriptors.AsSpan(0, MipCount);

        /// <summary>
        /// Returns a read-only span of descriptors.
        /// </summary>
        /// <param name="start">Start index of the span.</param>
        /// <param name="length">Number of elements in the span.</param>
        public ReadOnlySpan<RenderTextureDescriptor> AsSpan(int start, int length)
        {
            start = Math.Clamp(start, 0, MipCount);
            length = Math.Clamp(length, 0, MipCount - start);
            return m_Descriptors.AsSpan(start, length);
        }
    }
}