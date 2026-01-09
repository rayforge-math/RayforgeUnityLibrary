using Rayforge.Core.Diagnostics;
using UnityEngine;

namespace Rayforge.Core.Rendering.Passes
{
    /// <summary>
    /// Metadata describing a complete raster pass.
    /// Encapsulates <see cref="Material"/>, the pass index within the material,
    /// and optional <see cref="MaterialPropertyBlock"/> overrides.
    /// 
    /// Designed for use with RenderGraph or low-level raster pass execution.
    /// Assertions are used to catch developer mistakes; production will not throw.
    /// </summary>
    public struct RasterPassMeta
    {
        private Material material;
        private int passId;
        private MaterialPropertyBlock propertyBlock;

        /// <summary>
        /// The <see cref="Material"/> used for the raster pass.
        /// Cannot be null. Setting a null value triggers an assertion.
        /// </summary>
        public Material Material
        {
            get => material;
            set
            {
                Assertions.NotNull(value, "Material cannot be null.");
                material = value;
            }
        }

        /// <summary>
        /// The index of the pass within the material.
        /// Must be >= 0. Negative values trigger an assertion.
        /// </summary>
        public int PassId
        {
            get => passId;
            set
            {
                Assertions.AtLeastZero(value, "PassId must be non-negative.");
                passId = value;
            }
        }

        /// <summary>
        /// Optional <see cref="MaterialPropertyBlock"/> used to override shader properties for this pass.
        /// Can be null.
        /// </summary>
        public MaterialPropertyBlock PropertyBlock
        {
            get => propertyBlock;
            set => propertyBlock = value;
        }

        /// <summary>
        /// Constructs a <see cref="RasterMeta"/> from a <see cref="Material"/> and an explicit pass index.
        /// Uses property setters so assertions are triggered for invalid values.
        /// </summary>
        public RasterPassMeta(Material material, int passId, MaterialPropertyBlock propertyBlock = null)
        {
            this.material = null;
            this.passId = 0;
            this.propertyBlock = null;

            Material = material;
            PassId = passId;
            PropertyBlock = propertyBlock;
        }

        /// <summary>
        /// Constructs a <see cref="RasterMeta"/> from a <see cref="Material"/> and a pass name.
        /// Resolves the pass index automatically using <see cref="Material.FindPass"/>.
        /// Uses property setters and assertions for validation.
        /// </summary>
        public RasterPassMeta(Material material, string passName, MaterialPropertyBlock propertyBlock = null)
        {
            this.material = null;
            this.passId = 0;
            this.propertyBlock = null;

            Material = material;

            int id = -1;
            if (material != null && !string.IsNullOrEmpty(passName))
                id = material.FindPass(passName);

            Assertions.IsTrue(!string.IsNullOrEmpty(passName), "Pass name cannot be null or empty.");
            Assertions.IsTrue(id >= 0, $"Pass '{passName}' not found in material '{material?.name ?? "<null>"}'.");

            PassId = id;
            PropertyBlock = propertyBlock;
        }

        /// <summary>
        /// Returns true if the raster pass is valid.
        /// A valid pass requires a non-null <see cref="Material"/> and a non-negative <see cref="PassId"/>.
        /// </summary>
        public bool IsValid => Material != null && PassId >= 0;

        /// <summary>
        /// Returns a human-readable string describing this raster pass metadata.
        /// Includes material name, pass index, validity, and whether a property block is set.
        /// </summary>
        /// <returns>A string representation of the raster pass meta.</returns>
        public override string ToString()
        {
            string matName = Material != null ? Material.name : "<null>";
            string propBlockInfo = PropertyBlock != null ? "with PropertyBlock" : "no PropertyBlock";
            string validInfo = IsValid ? "Valid" : "Invalid";

            return $"RasterPassMeta: Material='{matName}', PassId={PassId}, {propBlockInfo}, {validInfo}";
        }
    }
}
