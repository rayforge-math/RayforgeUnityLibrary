using UnityEngine;

namespace Rayforge.Rendering.Passes
{
    /// <summary>
    /// Metadata for a raster pass dispatch, including the material, pass index, and optional property block.
    /// Wraps a <see cref="RasterMaterialMeta"/> for strongly typed material/pass info.
    /// </summary>
    public readonly struct RasterDispatchMeta
    {
        private readonly RasterMaterialMeta k_MaterialMeta;
        private readonly MaterialPropertyBlock k_PropertyBlock;

        /// <summary>
        /// The material used for this raster pass.
        /// </summary>
        public Material Material => k_MaterialMeta.Material;

        /// <summary>
        /// The material pass index to use when drawing.
        /// </summary>
        public int PassId => k_MaterialMeta.PassId;

        /// <summary>
        /// Optional material property block applied during this pass.
        /// </summary>
        public MaterialPropertyBlock PropertyBlock => k_PropertyBlock;

        /// <summary>
        /// Constructs a new <see cref="RasterDispatchMeta"/> from a material and explicit pass index.
        /// </summary>
        /// <param name="material">The material to use for the pass.</param>
        /// <param name="passId">The material pass index.</param>
        /// <param name="propertyBlock">Optional property block to override material properties.</param>
        public RasterDispatchMeta(Material material, int passId, MaterialPropertyBlock propertyBlock = null)
        {
            k_MaterialMeta = new RasterMaterialMeta(material, passId);
            k_PropertyBlock = propertyBlock;
        }

        /// <summary>
        /// Constructs a new <see cref="RasterDispatchMeta"/> from a material and pass name.
        /// Resolves the pass index from the material.
        /// </summary>
        /// <param name="material">The material to use for the pass.</param>
        /// <param name="passName">The name of the pass to resolve.</param>
        /// <param name="propertyBlock">Optional property block to override material properties.</param>
        public RasterDispatchMeta(Material material, string passName, MaterialPropertyBlock propertyBlock = null)
        {
            k_MaterialMeta = new RasterMaterialMeta(material, passName);
            k_PropertyBlock = propertyBlock;
        }

        /// <summary>
        /// Returns true if the material meta is valid (material not null and pass index non-negative).
        /// </summary>
        public bool IsValid => k_MaterialMeta.Material != null && k_MaterialMeta.PassId >= 0;
    }
}