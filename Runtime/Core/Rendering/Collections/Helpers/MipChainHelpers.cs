using Rayforge.Core.Diagnostics;
using System;
using UnityEngine;

namespace Rayforge.Core.Rendering.Collections.Helpers
{
    public static class MipChainHelpers
    {
        /// <summary>
        /// Default mip calculation: halve each dimension per level, clamped to 1.
        /// Ensures that resolution never drops below 1.
        /// </summary>
        /// <param name="mipLevel">Mipmap level (must be >= 0).</param>
        /// <param name="baseRes">Base resolution of the texture (must be positive).</param>
        /// <returns>Resolution of the mip level, clamped to at least 1 in each dimension.</returns>
        public static Vector2Int DefaultMipResolution(int mipLevel, Vector2Int baseRes)
        {
            Assertions.AtLeastZero(mipLevel, "Mipmap level must be >= 0.");
            Assertions.IsTrue(baseRes.x > 0 && baseRes.y > 0, "Base resolution must be positive.");

            int x = Math.Max(1, baseRes.x >> mipLevel);
            int y = Math.Max(1, baseRes.y >> mipLevel);
            return new Vector2Int(x, y);
        }
    }
}