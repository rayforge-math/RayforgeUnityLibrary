using Rayforge.Diagnostics;
using UnityEngine;

namespace Rayforge.Rendering.Passes
{
    /// <summary>
    /// Metadata for a single compute shader pass.
    /// Encapsulates the shader asset and the kernel index (resolved from name or explicitly set).
    /// </summary>
    public readonly struct ComputeKernelMeta
    {
        /// <summary>
        /// The compute shader asset to dispatch.
        /// </summary>
        public readonly ComputeShader Shader;

        /// <summary>
        /// The kernel index within the compute shader.
        /// </summary>
        public readonly int KernelIndex;

        /// <summary>
        /// Constructs a new <see cref="ComputePassMeta"/> by resolving the kernel index from a kernel name.
        /// </summary>
        /// <param name="shader">The compute shader asset.</param>
        /// <param name="kernelName">The name of the kernel to resolve.</param>
        public ComputeKernelMeta(ComputeShader shader, string kernelName)
        {
            Assertions.NotNull(shader, "ComputeShader cannot be null.");
            Assertions.IsFalse(string.IsNullOrEmpty(kernelName), "Kernel name cannot be null or empty.");

            Shader = shader;

            int index = shader.FindKernel(kernelName);
            Assertions.IsTrue(index >= 0, $"Kernel '{kernelName}' not found in compute shader '{shader.name}'.");
            KernelIndex = index;
        }

        /// <summary>
        /// Constructs a new <see cref="ComputePassMeta"/> by directly specifying the kernel index.
        /// </summary>
        /// <param name="shader">The compute shader asset.</param>
        /// <param name="kernelIndex">The kernel index to use.</param>
        public ComputeKernelMeta(ComputeShader shader, int kernelIndex)
        {
            Assertions.NotNull(shader, "ComputeShader cannot be null.");
            Assertions.AtLeastZero(kernelIndex, "KernelIndex must be non-negative.");

            Shader = shader;
            KernelIndex = kernelIndex;
        }

        /// <summary>
        /// Returns true if the shader is not null and the kernel index is valid.
        /// </summary>
        public bool IsValid => Shader != null && KernelIndex >= 0;

        /// <summary>
        /// Returns a string representation for debugging purposes.
        /// </summary>
        public override string ToString()
        {
            string shaderName = Shader != null ? Shader.name : "<null>";
            return $"{shaderName} [Kernel {KernelIndex}]";
        }
    }
}