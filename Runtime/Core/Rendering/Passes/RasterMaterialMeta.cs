using Rayforge.Diagnostics;
using UnityEngine;

namespace Rayforge.Rendering.Passes
{
    /// <summary>
    /// Encapsulates the material and pass index for a raster pass.
    /// Analog zu ComputeKernelMeta für ComputeShader.
    /// </summary>
    public readonly struct RasterMaterialMeta
    {
        /// <summary>
        /// The material used for this pass.
        /// </summary>
        public readonly Material Material;

        /// <summary>
        /// The material pass index to use.
        /// </summary>
        public readonly int PassId;

        /// <summary>
        /// Constructs a new <see cref="RasterMaterialMeta"/> with a material and pass index.
        /// </summary>
        /// <param name="material">The material to use for the pass.</param>
        /// <param name="passId">The material pass index.</param>
        public RasterMaterialMeta(Material material, int passId)
        {
            Assertions.NotNull(material, "Material cannot be null.");
            Assertions.AtLeastZero(passId, "PassId must be non-negative.");

            Material = material;
            PassId = passId;
        }

        /// <summary>
        /// Constructs a new <see cref="RasterMaterialMeta"/> by resolving the pass index from a pass name.
        /// </summary>
        /// <param name="material">The material to use for the pass.</param>
        /// <param name="passName">The name of the pass to resolve.</param>
        public RasterMaterialMeta(Material material, string passName)
        {
            Assertions.NotNull(material, "Material cannot be null.");
            Assertions.IsFalse(string.IsNullOrEmpty(passName), "Pass name cannot be null or empty.");

            Material = material;

            int id = material.FindPass(passName);
            Assertions.IsTrue(id >= 0, $"Pass '{passName}' not found in material '{material.name}'.");
            PassId = id;
        }
    }
}
