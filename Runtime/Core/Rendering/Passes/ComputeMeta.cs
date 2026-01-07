using Rayforge.Diagnostics;
using UnityEngine;

namespace Rayforge.Rendering.Passes
{
    /// <summary>
    /// Metadata describing a complete compute dispatch:
    /// shader, kernel, and thread group counts.
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
        /// </summary>
        public ComputeMeta(
            ComputeShader shader,
            string kernelName,
            int threadGroupsX,
            int threadGroupsY,
            int threadGroupsZ = 1)
        {
            Assertions.NotNull(shader, "ComputeShader cannot be null.");
            Assertions.IsFalse(string.IsNullOrEmpty(kernelName), "Kernel name cannot be null or empty.");

            Shader = shader;

            int index = shader.FindKernel(kernelName);
            Assertions.IsTrue(index >= 0, $"Kernel '{kernelName}' not found in compute shader '{shader.name}'.");

            KernelIndex = index;

            Assertions.IsTrue(threadGroupsX > 0, "ThreadGroupsX must be > 0.");
            Assertions.IsTrue(threadGroupsY > 0, "ThreadGroupsY must be > 0.");
            Assertions.IsTrue(threadGroupsZ > 0, "ThreadGroupsZ must be > 0.");

            ThreadGroupsX = threadGroupsX;
            ThreadGroupsY = threadGroupsY;
            ThreadGroupsZ = threadGroupsZ;
        }

        /// <summary>
        /// Construct from shader + kernel index.
        /// </summary>
        public ComputeMeta(
            ComputeShader shader,
            int kernelIndex,
            int threadGroupsX,
            int threadGroupsY,
            int threadGroupsZ = 1)
        {
            Assertions.NotNull(shader, "ComputeShader cannot be null.");
            Assertions.AtLeastZero(kernelIndex, "KernelIndex must be non-negative.");

            Shader = shader;
            KernelIndex = kernelIndex;

            Assertions.IsTrue(threadGroupsX > 0, "ThreadGroupsX must be > 0.");
            Assertions.IsTrue(threadGroupsY > 0, "ThreadGroupsY must be > 0.");
            Assertions.IsTrue(threadGroupsZ > 0, "ThreadGroupsZ must be > 0.");

            ThreadGroupsX = threadGroupsX;
            ThreadGroupsY = threadGroupsY;
            ThreadGroupsZ = threadGroupsZ;
        }

        /// <summary>
        /// Returns true if the meta describes a valid dispatch.
        /// </summary>
        public bool IsValid =>
            Shader != null &&
            KernelIndex >= 0 &&
            ThreadGroupsX > 0 &&
            ThreadGroupsY > 0 &&
            ThreadGroupsZ > 0;

        public override string ToString()
        {
            string shaderName = Shader != null ? Shader.name : "<null>";
            return $"{shaderName} [Kernel {KernelIndex}] TG({ThreadGroupsX},{ThreadGroupsY},{ThreadGroupsZ})";
        }
    }
}
