using UnityEngine;

namespace Rayforge.Core.Common
{
    public static class Globals
    {
        public const string CompanyName = "Rayforge";

        private static readonly Vector2Int k_BaseResolution = new Vector2Int { x = 1920, y = 1080 };
        public static Vector2Int BaseResolution => k_BaseResolution;

        private static readonly Vector2 k_BaseTexelSize = new Vector2 { x = 1.0f / k_BaseResolution.x, y = 1.0f / k_BaseResolution.y };
        public static Vector2 BaseTexelSize => k_BaseTexelSize;
    }
}
