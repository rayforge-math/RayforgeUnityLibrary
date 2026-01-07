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
    /// <typeparam name="Tcmd">
    /// Command buffer type, e.g., <see cref="RasterCommandBuffer"/> or <see cref="UnsafeCommandBuffer"/>.
    /// </typeparam>
    internal readonly struct RasterPassMetaInternal<Tcmd, Tdata>
        where Tcmd : BaseCommandBuffer
    {
        /// <summary>
        /// Core raster pass metadata (material, pass index, property block).
        /// </summary>
        public readonly RasterMeta Meta;

        /// <summary>
        /// Optional callback invoked during execution of this pass.
        /// </summary>
        public readonly Action<Tcmd, MaterialPropertyBlock, Tdata> UpdateCallback;

        public RasterPassMetaInternal(
            RasterMeta meta,
            Action<Tcmd, MaterialPropertyBlock, Tdata> updateCallback)
        {
            Meta = meta;
            UpdateCallback = updateCallback;
        }
    }

    /// <summary>
    /// Public, strongly typed wrapper for a raster pass executed on a <see cref="RasterCommandBuffer"/>.
    /// </summary>
    public readonly struct RasterPassMeta<Tdata>
    {
        private readonly RasterPassMetaInternal<RasterCommandBuffer, Tdata> k_Internal;

        public RasterPassMeta(
            Material material,
            int passId,
            MaterialPropertyBlock propertyBlock = null,
            Action<RasterCommandBuffer, MaterialPropertyBlock, Tdata> updateCallback = null)
        {
            k_Internal = new RasterPassMetaInternal<RasterCommandBuffer, Tdata>(
                new RasterMeta(material, passId, propertyBlock),
                updateCallback);
        }

        public RasterPassMeta(
            Material material,
            string passName,
            MaterialPropertyBlock propertyBlock = null,
            Action<RasterCommandBuffer, MaterialPropertyBlock, Tdata> updateCallback = null)
        {
            k_Internal = new RasterPassMetaInternal<RasterCommandBuffer, Tdata>(
                new RasterMeta(material, passName, propertyBlock),
                updateCallback);
        }

        /// <summary>Raster metadata (material, pass, property block).</summary>
        public RasterMeta Meta => k_Internal.Meta;

        /// <summary>Optional update callback.</summary>
        public Action<RasterCommandBuffer, MaterialPropertyBlock, Tdata> UpdateCallback
            => k_Internal.UpdateCallback;
    }

    /// <summary>
    /// Public, strongly typed wrapper for a raster pass executed on an <see cref="UnsafeCommandBuffer"/>.
    /// </summary>
    public readonly struct UnsafeRasterPassMeta<Tdata>
    {
        private readonly RasterPassMetaInternal<UnsafeCommandBuffer, Tdata> k_Internal;

        public UnsafeRasterPassMeta(
            Material material,
            int passId,
            MaterialPropertyBlock propertyBlock = null,
            Action<UnsafeCommandBuffer, MaterialPropertyBlock, Tdata> updateCallback = null)
        {
            k_Internal = new RasterPassMetaInternal<UnsafeCommandBuffer, Tdata>(
                new RasterMeta(material, passId, propertyBlock),
                updateCallback);
        }

        public UnsafeRasterPassMeta(
            Material material,
            string passName,
            MaterialPropertyBlock propertyBlock = null,
            Action<UnsafeCommandBuffer, MaterialPropertyBlock, Tdata> updateCallback = null)
        {
            k_Internal = new RasterPassMetaInternal<UnsafeCommandBuffer, Tdata>(
                new RasterMeta(material, passName, propertyBlock),
                updateCallback);
        }

        /// <summary>Raster metadata (material, pass, property block).</summary>
        public RasterMeta Meta => k_Internal.Meta;

        /// <summary>Optional update callback.</summary>
        public Action<UnsafeCommandBuffer, MaterialPropertyBlock, Tdata> UpdateCallback
            => k_Internal.UpdateCallback;
    }
}
