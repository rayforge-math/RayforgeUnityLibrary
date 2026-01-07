using Rayforge.Rendering.Passes;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Internal wrapper for a raster pass that may include a callback.
    /// Not intended for external use; encapsulates the callback handling.
    /// </summary>
    /// <typeparam name="Tcmd">Command buffer type, e.g., <see cref="RasterCommandBuffer"/> or <see cref="UnsafeCommandBuffer"/>.</typeparam>
    internal readonly struct RasterPassMetaInternal<Tcmd>
        where Tcmd : BaseCommandBuffer
    {
        /// <summary>
        /// Core data of the raster pass, including material, pass index, and optional property block.
        /// </summary>
        public readonly RasterDispatchMeta DispatchMeta;

        /// <summary>
        /// Optional callback invoked during execution of this pass.
        /// Receives the specific command buffer and a <see cref="MaterialPropertyBlock"/>.
        /// </summary>
        public readonly Action<Tcmd, MaterialPropertyBlock> UpdateCallback;

        /// <summary>
        /// Constructs a new internal raster pass meta.
        /// </summary>
        /// <param name="dispatchMeta">The dispatch metadata containing material, pass, and optional property block.</param>
        /// <param name="callback">Optional update callback to be executed before the draw.</param>
        public RasterPassMetaInternal(RasterDispatchMeta dispatchMeta, Action<Tcmd, MaterialPropertyBlock> callback = null)
        {
            DispatchMeta = dispatchMeta;
            UpdateCallback = callback;
        }
    }

    /// <summary>
    /// Public, strongly typed wrapper for a raster pass executed on a <see cref="RasterCommandBuffer"/>.
    /// Hides the internal generic metadata and provides user-friendly constructors.
    /// </summary>
    public readonly struct RasterPassMeta
    {
        /// <summary>
        /// Internal metadata storing dispatch info and callback.
        /// </summary>
        private readonly RasterPassMetaInternal<RasterCommandBuffer> k_MetaInternal;

        /// <summary>
        /// Constructs a new <see cref="RasterPassMeta"/> from a material and explicit pass index.
        /// </summary>
        /// <param name="material">The material to use for this pass.</param>
        /// <param name="passId">The material pass index to use.</param>
        /// <param name="propertyBlock">Optional <see cref="MaterialPropertyBlock"/> to override material properties.</param>
        /// <param name="updateCallback">Optional callback executed with the <see cref="RasterCommandBuffer"/> before the draw.</param>
        public RasterPassMeta(
            Material material,
            int passId,
            MaterialPropertyBlock propertyBlock = null,
            Action<RasterCommandBuffer, MaterialPropertyBlock> updateCallback = null)
            : this(new RasterDispatchMeta(material, passId, propertyBlock), updateCallback)
        { }

        /// <summary>
        /// Constructs a new <see cref="RasterPassMeta"/> from an existing <see cref="RasterDispatchMeta"/>.
        /// </summary>
        /// <param name="dispatchMeta">The dispatch metadata containing material, pass index, and optional property block.</param>
        /// <param name="updateCallback">Optional callback executed with the <see cref="RasterCommandBuffer"/> before the draw.</param>
        public RasterPassMeta(
            RasterDispatchMeta dispatchMeta,
            Action<RasterCommandBuffer, MaterialPropertyBlock> updateCallback = null)
        {
            k_MetaInternal = new RasterPassMetaInternal<RasterCommandBuffer>(dispatchMeta, updateCallback);
        }

        /// <summary>
        /// Gets the dispatch metadata for this pass.
        /// Contains the material, pass index, and optional property block.
        /// </summary>
        public RasterDispatchMeta DispatchMeta => k_MetaInternal.DispatchMeta;

        /// <summary>
        /// Gets the optional update callback, invoked before drawing this pass.
        /// </summary>
        public Action<RasterCommandBuffer, MaterialPropertyBlock> UpdateCallback => k_MetaInternal.UpdateCallback;
    }

    /// <summary>
    /// Public, strongly typed wrapper for a raster pass executed on an <see cref="UnsafeCommandBuffer"/>.
    /// Hides the internal generic metadata and provides user-friendly constructors.
    /// </summary>
    public readonly struct UnsafeRasterPassMeta
    {
        /// <summary>
        /// Internal metadata storing dispatch info and callback.
        /// </summary>
        private readonly RasterPassMetaInternal<UnsafeCommandBuffer> k_MetaInternal;

        /// <summary>
        /// Constructs a new <see cref="UnsafeRasterPassMeta"/> from a material and explicit pass index.
        /// </summary>
        /// <param name="material">The material to use for this pass.</param>
        /// <param name="passId">The material pass index to use.</param>
        /// <param name="propertyBlock">Optional <see cref="MaterialPropertyBlock"/> to override material properties.</param>
        /// <param name="updateCallback">Optional callback executed with the <see cref="UnsafeCommandBuffer"/> before the draw.</param>
        public UnsafeRasterPassMeta(
            Material material,
            int passId,
            MaterialPropertyBlock propertyBlock = null,
            Action<UnsafeCommandBuffer, MaterialPropertyBlock> updateCallback = null)
            : this(new RasterDispatchMeta(material, passId, propertyBlock), updateCallback)
        { }

        /// <summary>
        /// Constructs a new <see cref="UnsafeRasterPassMeta"/> from an existing <see cref="RasterDispatchMeta"/>.
        /// </summary>
        /// <param name="dispatchMeta">The dispatch metadata containing material, pass index, and optional property block.</param>
        /// <param name="updateCallback">Optional callback executed with the <see cref="UnsafeCommandBuffer"/> before the draw.</param>
        public UnsafeRasterPassMeta(
            RasterDispatchMeta dispatchMeta,
            Action<UnsafeCommandBuffer, MaterialPropertyBlock> updateCallback = null)
        {
            k_MetaInternal = new RasterPassMetaInternal<UnsafeCommandBuffer>(dispatchMeta, updateCallback);
        }

        /// <summary>
        /// Gets the dispatch metadata for this pass.
        /// Contains the material, pass index, and optional property block.
        /// </summary>
        public RasterDispatchMeta DispatchMeta => k_MetaInternal.DispatchMeta;

        /// <summary>
        /// Gets the optional update callback, invoked before drawing this pass.
        /// </summary>
        public Action<UnsafeCommandBuffer, MaterialPropertyBlock> UpdateCallback => k_MetaInternal.UpdateCallback;
    }
}
