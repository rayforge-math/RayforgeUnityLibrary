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
    public readonly struct ComputePassMeta<TData>
    {
        /// <summary>
        /// Core compute metadata (shader, kernel, thread groups).
        /// </summary>
        public readonly ComputeMeta Meta;

        /// <summary>
        /// Optional callback invoked before dispatch to bind resources, set constants, etc.
        /// </summary>
        public readonly Action<ComputeCommandBuffer, TData> UpdateCallback;

        /// <summary>
        /// Initializes a new instance of <see cref="ComputePassMeta{TData}"/> from an existing <see cref="ComputeMeta"/>.
        /// </summary>
        /// <param name="meta">The core compute metadata.</param>
        /// <param name="updateCallback">Optional callback invoked before dispatch.</param>
        public ComputePassMeta(
            ComputeMeta meta,
            Action<ComputeCommandBuffer, TData> updateCallback = null)
        {
            Meta = meta;
            UpdateCallback = updateCallback;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ComputePassMeta{TData}"/> from a shader and kernel name.
        /// </summary>
        /// <param name="shader">The compute shader to execute.</param>
        /// <param name="kernelName">The name of the kernel to dispatch.</param>
        /// <param name="threadGroupsX">Number of thread groups in the X dimension.</param>
        /// <param name="threadGroupsY">Number of thread groups in the Y dimension.</param>
        /// <param name="threadGroupsZ">Number of thread groups in the Z dimension.</param>
        /// <param name="updateCallback">Optional callback invoked before dispatch.</param>
        public ComputePassMeta(
            ComputeShader shader,
            string kernelName,
            int threadGroupsX = 1,
            int threadGroupsY = 1,
            int threadGroupsZ = 1,
            Action<ComputeCommandBuffer, TData> updateCallback = null)
            : this(
                new ComputeMeta(shader, kernelName, threadGroupsX, threadGroupsY, threadGroupsZ),
                updateCallback)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="ComputePassMeta{TData}"/> from a shader and kernel index.
        /// </summary>
        /// <param name="shader">The compute shader to execute.</param>
        /// <param name="kernelIndex">The index of the kernel to dispatch.</param>
        /// <param name="threadGroupsX">Number of thread groups in the X dimension.</param>
        /// <param name="threadGroupsY">Number of thread groups in the Y dimension.</param>
        /// <param name="threadGroupsZ">Number of thread groups in the Z dimension.</param>
        /// <param name="updateCallback">Optional callback invoked before dispatch.</param>
        public ComputePassMeta(
            ComputeShader shader,
            int kernelIndex,
            int threadGroupsX = 1,
            int threadGroupsY = 1,
            int threadGroupsZ = 1,
            Action<ComputeCommandBuffer, TData> updateCallback = null)
            : this(
                new ComputeMeta(shader, kernelIndex, threadGroupsX, threadGroupsY, threadGroupsZ),
                updateCallback)
        { }
    }
}
