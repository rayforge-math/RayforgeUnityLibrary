using Rayforge.Diagnostics;
using System;
using UnityEngine;
using static UnityEditor.ShaderData;

namespace Rayforge.Rendering.Passes
{
    /// <summary>
    /// Immutable metadata describing a complete raster pass.
    /// Encapsulates the <see cref="Material"/>, the pass index within the material,
    /// and optional <see cref="MaterialPropertyBlock"/> overrides.
    /// 
    /// Designed for use with RenderGraph or low-level raster pass execution.
    /// </summary>
    public struct RasterMeta
    {
        /// <summary>
        /// The <see cref="Material"/> used for the raster pass.
        /// Must not be null.
        /// </summary>
        public Material Material;

        /// <summary>
        /// The index of the pass within the material.
        /// Must be greater than or equal to zero.
        /// </summary>
        public int PassId;

        /// <summary>
        /// Optional <see cref="MaterialPropertyBlock"/> used to override shader properties for this pass.
        /// Can be null.
        /// </summary>
        public MaterialPropertyBlock PropertyBlock;

        /// <summary>
        /// Constructs a <see cref="RasterMeta"/> from a <see cref="Material"/> and an explicit pass index.
        /// </summary>
        /// <param name="material">The <see cref="Material"/> to use. Must not be null.</param>
        /// <param name="passId">The pass index within the material. Must be >= 0.</param>
        /// <param name="propertyBlock">Optional property block for overriding material properties. Can be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="material"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="passId"/> is negative.</exception>
        public RasterMeta(
            Material material,
            int passId,
            MaterialPropertyBlock propertyBlock = null)
        {
            if (material == null)
                throw new ArgumentNullException(nameof(material), "Material cannot be null.");
            if (passId < 0)
                throw new ArgumentOutOfRangeException(nameof(passId), "PassId must be non-negative.");

            Material = material;
            PassId = passId;
            PropertyBlock = propertyBlock;
        }

        /// <summary>
        /// Constructs a <see cref="RasterMeta"/> from a <see cref="Material"/> and a pass name.
        /// Resolves the pass index automatically using <see cref="Material.FindPass"/>.
        /// </summary>
        /// <param name="material">The <see cref="Material"/> to use. Must not be null.</param>
        /// <param name="passName">The name of the pass to find. Must not be null or empty and must exist in the material.</param>
        /// <param name="propertyBlock">Optional property block for overriding material properties. Can be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="material"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="passName"/> is null, empty, or not found in the material.</exception>
        public RasterMeta(
            Material material,
            string passName,
            MaterialPropertyBlock propertyBlock = null)
        {
            if (material == null)
                throw new ArgumentNullException(nameof(material), "Material cannot be null.");
            if (string.IsNullOrEmpty(passName))
                throw new ArgumentException("Pass name cannot be null or empty.", nameof(passName));

            int id = material.FindPass(passName);
            if (id < 0)
                throw new ArgumentException($"Pass '{passName}' not found in material '{material.name}'.", nameof(passName));

            Material = material;
            PassId = id;
            PropertyBlock = propertyBlock;
        }

        /// <summary>
        /// Returns true if the raster pass is valid.
        /// A valid pass requires a non-null <see cref="Material"/> and a non-negative <see cref="PassId"/>.
        /// </summary>
        public bool IsValid =>
            Material != null &&
            PassId >= 0;

        /// <summary>
        /// Returns a string representation of the raster pass, including material name and pass index.
        /// </summary>
        public override string ToString()
        {
            string matName = Material != null ? Material.name : "<null>";
            return $"{matName} [Pass {PassId}]";
        }
    }
}
