using Rayforge.Diagnostics;
using UnityEngine;

namespace Rayforge.Rendering.Passes
{
    /// <summary>
    /// Metadata describing a complete raster pass:
    /// material, pass index, and optional property block.
    /// </summary>
    public struct RasterMeta
    {
        /// <summary>The material used for the raster pass.</summary>
        public Material Material;

        /// <summary>The material pass index.</summary>
        public int PassId;

        /// <summary>Optional material property overrides.</summary>
        public MaterialPropertyBlock PropertyBlock;

        /// <summary>
        /// Construct from material and explicit pass index.
        /// </summary>
        public RasterMeta(
            Material material,
            int passId,
            MaterialPropertyBlock propertyBlock = null)
        {
            Assertions.NotNull(material, "Material cannot be null.");
            Assertions.AtLeastZero(passId, "PassId must be non-negative.");

            Material = material;
            PassId = passId;
            PropertyBlock = propertyBlock;
        }

        /// <summary>
        /// Construct from material and pass name.
        /// Resolves the pass index from the material.
        /// </summary>
        public RasterMeta(
            Material material,
            string passName,
            MaterialPropertyBlock propertyBlock = null)
        {
            Assertions.NotNull(material, "Material cannot be null.");
            Assertions.IsFalse(string.IsNullOrEmpty(passName), "Pass name cannot be null or empty.");

            int id = material.FindPass(passName);
            Assertions.IsTrue(id >= 0, $"Pass '{passName}' not found in material '{material.name}'.");

            Material = material;
            PassId = id;
            PropertyBlock = propertyBlock;
        }

        /// <summary>
        /// Returns true if the raster pass is valid.
        /// </summary>
        public bool IsValid =>
            Material != null &&
            PassId >= 0;

        public override string ToString()
        {
            string matName = Material != null ? Material.name : "<null>";
            return $"{matName} [Pass {PassId}]";
        }
    }
}
