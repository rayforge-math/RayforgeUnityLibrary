using Rayforge.Diagnostics;
using System;
using UnityEngine;

namespace Rayforge.Rendering.Passes
{
    /// <summary>
    /// Metadata describing a complete compute dispatch:
    /// shader, kernel, and thread group counts.
    /// Ensures that all values are valid for a dispatch.
    /// </summary>
    public struct ComputeMeta
    {
        /// <summary>The compute shader asset.</summary>
        public ComputeShader Shader;

        /// <summary>The kernel index within the shader.</summary>
        public int KernelIndex;

        /// <summary>Thread groups in X dimension.</summary>
        public int ThreadGroupsX;

        /// <summary>Thread groups in Y dimension.</summary>
        public int ThreadGroupsY;

        /// <summary>Thread groups in Z dimension.</summary>
        public int ThreadGroupsZ;

        /// <summary>
        /// Construct from shader + kernel name.
        /// Exceptions are thrown for invalid arguments.
        /// Assertions are used in editor/dev builds for extra developer feedback.
        /// </summary>
        /// <param name="shader">Compute shader to dispatch.</param>
        /// <param name="kernelName">Name of the kernel to dispatch.</param>
        /// <param name="threadGroupsX">Number of thread groups in X dimension (>0).</param>
        /// <param name="threadGroupsY">Number of thread groups in Y dimension (>0).</param>
        /// <param name="threadGroupsZ">Number of thread groups in Z dimension (>0).</param>
        /// <exception cref="ArgumentNullException">Thrown if shader is null.</exception>
        /// <exception cref="ArgumentException">Thrown if kernel not found or thread groups invalid.</exception>
        public ComputeMeta(
            ComputeShader shader,
            string kernelName,
            int threadGroupsX,
            int threadGroupsY,
            int threadGroupsZ = 1)
        {
            if (shader == null) 
                throw new ArgumentNullException(nameof(shader));
            if (string.IsNullOrEmpty(kernelName)) 
                throw new ArgumentException("Kernel name cannot be null or empty.", nameof(kernelName));

            int index = shader.FindKernel(kernelName);
            if (index < 0) 
                throw new ArgumentException($"Kernel '{kernelName}' not found in shader '{shader.name}'.", nameof(kernelName));

            if (threadGroupsX <= 0 || threadGroupsY <= 0 || threadGroupsZ <= 0)
                throw new ArgumentException("Thread group counts must be > 0.");

            Shader = shader;
            KernelIndex = index;
            ThreadGroupsX = threadGroupsX;
            ThreadGroupsY = threadGroupsY;
            ThreadGroupsZ = threadGroupsZ;
        }

        /// <summary>
        /// Construct from shader + kernel index.
        /// Throws exceptions for invalid arguments.
        /// </summary>
        /// <param name="shader">Compute shader to dispatch.</param>
        /// <param name="kernelIndex">Kernel index (>=0).</param>
        /// <param name="threadGroupsX">Number of thread groups in X dimension (>0).</param>
        /// <param name="threadGroupsY">Number of thread groups in Y dimension (>0).</param>
        /// <param name="threadGroupsZ">Number of thread groups in Z dimension (>0).</param>
        /// <exception cref="ArgumentNullException">Thrown if shader is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if kernelIndex < 0.</exception>
        /// <exception cref="ArgumentException">Thrown if thread groups invalid.</exception>
        public ComputeMeta(
            ComputeShader shader,
            int kernelIndex,
            int threadGroupsX,
            int threadGroupsY,
            int threadGroupsZ = 1)
        {
            if (shader == null) 
                throw new ArgumentNullException(nameof(shader));
            if (kernelIndex < 0) 
                throw new ArgumentOutOfRangeException(nameof(kernelIndex), "Kernel index must be >= 0.");
            if (threadGroupsX <= 0 || threadGroupsY <= 0 || threadGroupsZ <= 0)
                throw new ArgumentException("Thread group counts must be > 0.");

            Shader = shader;
            KernelIndex = kernelIndex;
            ThreadGroupsX = threadGroupsX;
            ThreadGroupsY = threadGroupsY;
            ThreadGroupsZ = threadGroupsZ;
        }

        /// <summary>
        /// Returns true if the meta describes a valid dispatch.
        /// Can be used for conditional checks without throwing exceptions.
        /// </summary>
        public bool IsValid =>
            Shader != null &&
            KernelIndex >= 0 &&
            ThreadGroupsX > 0 &&
            ThreadGroupsY > 0 &&
            ThreadGroupsZ > 0;
    }
}
