using System;
using UnityEngine;

namespace Rayforge.Rendering.Collections.Helpers
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
}