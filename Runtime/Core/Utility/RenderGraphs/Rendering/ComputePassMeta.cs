using Rayforge.Rendering.Passes;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Metadata for a compute pass execution.
    /// Wraps <see cref="ComputeMeta"/> and an optional pre-dispatch callback.
    /// </summary>
    public readonly struct ComputePassMeta<Tdata>
    {
        /// <summary>
        /// Core compute metadata (shader, kernel, thread groups).
        /// </summary>
        public readonly ComputeMeta Meta;

        /// <summary>
        /// Optional callback invoked before dispatch.
        /// Use this to bind resources, constants, etc.
        /// </summary>
        public readonly Action<ComputeCommandBuffer, Tdata> UpdateCallback;

        /// <summary>
        /// Constructs a new <see cref="ComputePassMeta"/> from an existing <see cref="ComputeMeta"/>.
        /// </summary>
        public ComputePassMeta(
            ComputeMeta meta,
            Action<ComputeCommandBuffer, Tdata> updateCallback = null)
        {
            Meta = meta;
            UpdateCallback = updateCallback;
        }

        /// <summary>
        /// Constructs a new <see cref="ComputePassMeta"/> from a shader, kernel name and thread group counts.
        /// </summary>
        public ComputePassMeta(
            ComputeShader shader,
            string kernelName,
            int threadGroupsX = 1,
            int threadGroupsY = 1,
            int threadGroupsZ = 1,
            Action<ComputeCommandBuffer, Tdata> updateCallback = null)
            : this(
                new ComputeMeta(shader, kernelName, threadGroupsX, threadGroupsY, threadGroupsZ),
                updateCallback)
        { }

        /// <summary>
        /// Constructs a new <see cref="ComputePassMeta"/> from a shader, kernel index and thread group counts.
        /// </summary>
        public ComputePassMeta(
            ComputeShader shader,
            int kernelIndex,
            int threadGroupsX = 1,
            int threadGroupsY = 1,
            int threadGroupsZ = 1,
            Action<ComputeCommandBuffer, Tdata> updateCallback = null)
            : this(
                new ComputeMeta(shader, kernelIndex, threadGroupsX, threadGroupsY, threadGroupsZ),
                updateCallback)
        { }
    }
}
