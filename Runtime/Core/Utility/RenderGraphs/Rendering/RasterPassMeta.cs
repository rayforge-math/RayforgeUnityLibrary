using Rayforge.Rendering.Passes;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Internal wrapper for a raster pass with optional callback support.
    /// Not intended for external use; encapsulates callback handling for different command buffer types.
    /// </summary>
    /// <typeparam name="TCmd">
    /// Command buffer type, e.g., <see cref="RasterCommandBuffer"/> or <see cref="UnsafeCommandBuffer"/>.
    /// </typeparam>
    /// <typeparam name="TData">Custom data type passed to the callback.</typeparam>
    internal readonly struct RasterPassMetaInternal<TCmd, TData>
        where TCmd : BaseCommandBuffer
    {
        /// <summary>
        /// Core raster pass metadata (material, pass index, property block).
        /// </summary>
        public readonly RasterMeta Meta;

        /// <summary>
        /// Optional callback invoked during pass execution to configure material properties.
        /// </summary>
        public readonly Action<TCmd, MaterialPropertyBlock, TData> UpdateCallback;

        public RasterPassMetaInternal(
            RasterMeta meta,
            Action<TCmd, MaterialPropertyBlock, TData> updateCallback)
        {
            Meta = meta;
            UpdateCallback = updateCallback;
        }
    }

    /// <summary>
    /// Metadata for a raster pass executed on a <see cref="RasterCommandBuffer"/>.
    /// Wraps <see cref="RasterMeta"/> and an optional material setup callback.
    /// </summary>
    /// <typeparam name="TData">Custom data type passed to the callback.</typeparam>
    public readonly struct RasterPassMeta<TData>
    {
        private readonly RasterPassMetaInternal<RasterCommandBuffer, TData> k_Internal;

        /// <summary>
        /// Initializes a new instance of <see cref="RasterPassMeta{TData}"/> using a material and pass index.
        /// </summary>
        /// <param name="material">The material to render with.</param>
        /// <param name="passId">The shader pass index to execute.</param>
        /// <param name="propertyBlock">Optional material property block for per-draw properties.</param>
        /// <param name="updateCallback">Optional callback invoked before rendering to configure material properties.</param>
        public RasterPassMeta(
            Material material,
            int passId,
            MaterialPropertyBlock propertyBlock = null,
            Action<RasterCommandBuffer, MaterialPropertyBlock, TData> updateCallback = null)
        {
            k_Internal = new RasterPassMetaInternal<RasterCommandBuffer, TData>(
                new RasterMeta(material, passId, propertyBlock),
                updateCallback);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RasterPassMeta{TData}"/> using a material and pass name.
        /// </summary>
        /// <param name="material">The material to render with.</param>
        /// <param name="passName">The shader pass name to execute.</param>
        /// <param name="propertyBlock">Optional material property block for per-draw properties.</param>
        /// <param name="updateCallback">Optional callback invoked before rendering to configure material properties.</param>
        public RasterPassMeta(
            Material material,
            string passName,
            MaterialPropertyBlock propertyBlock = null,
            Action<RasterCommandBuffer, MaterialPropertyBlock, TData> updateCallback = null)
        {
            k_Internal = new RasterPassMetaInternal<RasterCommandBuffer, TData>(
                new RasterMeta(material, passName, propertyBlock),
                updateCallback);
        }

        /// <summary>
        /// Gets the core raster metadata (material, pass, property block).
        /// </summary>
        public RasterMeta Meta => k_Internal.Meta;

        /// <summary>
        /// Gets the optional callback invoked before rendering.
        /// </summary>
        public Action<RasterCommandBuffer, MaterialPropertyBlock, TData> UpdateCallback
            => k_Internal.UpdateCallback;
    }

    /// <summary>
    /// Metadata for a raster pass executed on an <see cref="UnsafeCommandBuffer"/>.
    /// Provides low-level command buffer access with <see cref="RasterMeta"/> and an optional callback.
    /// </summary>
    /// <typeparam name="TData">Custom data type passed to the callback.</typeparam>
    public readonly struct UnsafeRasterPassMeta<TData>
    {
        private readonly RasterPassMetaInternal<UnsafeCommandBuffer, TData> k_Internal;

        /// <summary>
        /// Initializes a new instance of <see cref="UnsafeRasterPassMeta{TData}"/> using a material and pass index.
        /// </summary>
        /// <param name="material">The material to render with.</param>
        /// <param name="passId">The shader pass index to execute.</param>
        /// <param name="propertyBlock">Optional material property block for per-draw properties.</param>
        /// <param name="updateCallback">Optional callback invoked before rendering for low-level command buffer operations.</param>
        public UnsafeRasterPassMeta(
            Material material,
            int passId,
            MaterialPropertyBlock propertyBlock = null,
            Action<UnsafeCommandBuffer, MaterialPropertyBlock, TData> updateCallback = null)
        {
            k_Internal = new RasterPassMetaInternal<UnsafeCommandBuffer, TData>(
                new RasterMeta(material, passId, propertyBlock),
                updateCallback);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="UnsafeRasterPassMeta{TData}"/> using a material and pass name.
        /// </summary>
        /// <param name="material">The material to render with.</param>
        /// <param name="passName">The shader pass name to execute.</param>
        /// <param name="propertyBlock">Optional material property block for per-draw properties.</param>
        /// <param name="updateCallback">Optional callback invoked before rendering for low-level command buffer operations.</param>
        public UnsafeRasterPassMeta(
            Material material,
            string passName,
            MaterialPropertyBlock propertyBlock = null,
            Action<UnsafeCommandBuffer, MaterialPropertyBlock, TData> updateCallback = null)
        {
            k_Internal = new RasterPassMetaInternal<UnsafeCommandBuffer, TData>(
                new RasterMeta(material, passName, propertyBlock),
                updateCallback);
        }

        /// <summary>
        /// Gets the core raster metadata (material, pass, property block).
        /// </summary>
        public RasterMeta Meta => k_Internal.Meta;

        /// <summary>
        /// Gets the optional callback invoked before rendering.
        /// </summary>
        public Action<UnsafeCommandBuffer, MaterialPropertyBlock, TData> UpdateCallback
            => k_Internal.UpdateCallback;
    }
}
