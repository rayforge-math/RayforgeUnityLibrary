using System;
using UnityEngine;

using Rayforge.Infrastructure.Collections.Helpers;

namespace Rayforge.Infrastructure.Collections
{
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
}