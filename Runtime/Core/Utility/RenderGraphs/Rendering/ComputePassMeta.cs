using Rayforge.Rendering.Passes;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Metadata for a compute pass dispatch, including the kernel meta and an optional pre-dispatch callback.
    /// Encapsulates shader, kernel index, thread group counts, and optional parameter setup via callback.
    /// </summary>
    public readonly struct ComputePassMeta
    {
        /// <summary>
        /// The compute kernel metadata, including shader, kernel index, and thread group counts.
        /// </summary>
        public readonly ComputeDispatchMeta DispatchMeta;

        /// <summary>
        /// Optional callback invoked before dispatch.
        /// Receives the <see cref="ComputeCommandBuffer"/> for setting shader parameters, resources, or constants.
        /// </summary>
        public readonly Action<ComputeCommandBuffer> UpdateCallback;

        /// <summary>
        /// Constructs a new <see cref="ComputePassMeta"/> from a pre-existing <see cref="ComputeDispatchMeta"/>.
        /// </summary>
        /// <param name="dispatchMeta">The dispatch metadata containing shader, kernel index, and thread groups.</param>
        /// <param name="updateCallback">Optional callback executed with the <see cref="ComputeCommandBuffer"/> before dispatch.</param>
        public ComputePassMeta(ComputeDispatchMeta dispatchMeta, Action<ComputeCommandBuffer> updateCallback = null)
        {
            DispatchMeta = dispatchMeta;
            UpdateCallback = updateCallback;
        }

        /// <summary>
        /// Constructs a new <see cref="ComputePassMeta"/> from a compute shader, kernel name, and thread group counts.
        /// Resolves the kernel index from the provided name.
        /// </summary>
        /// <param name="shader">The compute shader asset to dispatch.</param>
        /// <param name="kernelName">The name of the kernel to resolve.</param>
        /// <param name="threadGroupsX">Number of thread groups in X dimension.</param>
        /// <param name="threadGroupsY">Number of thread groups in Y dimension.</param>
        /// <param name="threadGroupsZ">Number of thread groups in Z dimension.</param>
        /// <param name="updateCallback">Optional callback executed before dispatch.</param>
        public ComputePassMeta(
            ComputeShader shader,
            string kernelName,
            int threadGroupsX,
            int threadGroupsY,
            int threadGroupsZ,
            Action<ComputeCommandBuffer> updateCallback = null)
            : this(new ComputeDispatchMeta(shader, kernelName, threadGroupsX, threadGroupsY, threadGroupsZ), updateCallback)
        { }

        /// <summary>
        /// Constructs a new <see cref="ComputePassMeta"/> from a compute shader, kernel index, and thread group counts.
        /// </summary>
        /// <param name="shader">The compute shader asset to dispatch.</param>
        /// <param name="kernelIndex">The kernel index to use.</param>
        /// <param name="threadGroupsX">Number of thread groups in X dimension.</param>
        /// <param name="threadGroupsY">Number of thread groups in Y dimension.</param>
        /// <param name="threadGroupsZ">Number of thread groups in Z dimension.</param>
        /// <param name="updateCallback">Optional callback executed before dispatch.</param>
        public ComputePassMeta(
            ComputeShader shader,
            int kernelIndex,
            int threadGroupsX,
            int threadGroupsY,
            int threadGroupsZ,
            Action<ComputeCommandBuffer> updateCallback = null)
            : this(new ComputeDispatchMeta(shader, kernelIndex, threadGroupsX, threadGroupsY, threadGroupsZ), updateCallback)
        { }
    }
}
