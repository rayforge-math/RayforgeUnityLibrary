using Rayforge.Core.Diagnostics;
using UnityEngine;

namespace Rayforge.Core.Rendering.Passes
{
    /// <summary>
    /// Metadata describing a complete compute dispatch:
    /// shader, kernel, and thread group counts.
    /// Ensures that all values are valid for a dispatch.
    /// Assertions are used in editor/dev builds for developer feedback.
    /// </summary>
    public struct ComputePassMeta
    {
        private ComputeShader shader;
        private int kernelIndex;
        private int threadGroupsX;
        private int threadGroupsY;
        private int threadGroupsZ;

        /// <summary>
        /// The compute shader asset. Cannot be null.
        /// </summary>
        public ComputeShader Shader
        {
            get => shader;
            set
            {
                Assertions.NotNull(value, "Shader cannot be null.");
                shader = value;
            }
        }

        /// <summary>
        /// The kernel index within the shader. Must be >= 0.
        /// </summary>
        public int KernelIndex
        {
            get => kernelIndex;
            set
            {
                Assertions.AtLeastZero(value, "KernelIndex must be >= 0.");
                kernelIndex = value;
            }
        }

        /// <summary>
        /// Thread groups in X dimension. Must be > 0.
        /// </summary>
        public int ThreadGroupsX
        {
            get => threadGroupsX;
            set
            {
                Assertions.AtLeastOne(value, "ThreadGroupsX must be > 0.");
                threadGroupsX = value;
            }
        }

        /// <summary>
        /// Thread groups in Y dimension. Must be > 0.
        /// </summary>
        public int ThreadGroupsY
        {
            get => threadGroupsY;
            set
            {
                Assertions.AtLeastOne(value, "ThreadGroupsY must be > 0.");
                threadGroupsY = value;
            }
        }

        /// <summary>
        /// Thread groups in Z dimension. Must be > 0.
        /// </summary>
        public int ThreadGroupsZ
        {
            get => threadGroupsZ;
            set
            {
                Assertions.AtLeastOne(value, "ThreadGroupsZ must be > 0.");
                threadGroupsZ = value;
            }
        }

        /// <summary>
        /// Construct from shader + kernel name.
        /// Assertions ensure developer mistakes are caught.
        /// </summary>
        public ComputePassMeta(
            ComputeShader shader,
            string kernelName,
            int threadGroupsX = 1,
            int threadGroupsY = 1,
            int threadGroupsZ = 1)
        {
            this.shader = null;
            this.kernelIndex = 0;
            this.threadGroupsX = 0;
            this.threadGroupsY = 0;
            this.threadGroupsZ = 0;

            Shader = shader;

            Assertions.IsTrue(!string.IsNullOrEmpty(kernelName), "Kernel name cannot be null or empty.");
            int index = shader != null ? shader.FindKernel(kernelName) : -1;
            Assertions.IsTrue(index >= 0, $"Kernel '{kernelName}' not found in shader '{shader?.name ?? "<null>"}'.");

            KernelIndex = index;
            ThreadGroupsX = threadGroupsX;
            ThreadGroupsY = threadGroupsY;
            ThreadGroupsZ = threadGroupsZ;
        }

        /// <summary>
        /// Construct from shader + kernel index.
        /// Assertions ensure developer mistakes are caught.
        /// </summary>
        public ComputePassMeta(
            ComputeShader shader,
            int kernelIndex,
            int threadGroupsX = 1,
            int threadGroupsY = 1,
            int threadGroupsZ = 1)
        {
            this.shader = null;
            this.kernelIndex = 0;
            this.threadGroupsX = 0;
            this.threadGroupsY = 0;
            this.threadGroupsZ = 0;

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

        /// <summary>
        /// Returns a human-readable string describing this compute pass metadata.
        /// Includes shader name, kernel index, and thread group counts.
        /// </summary>
        /// <returns>A string representation of the compute pass meta.</returns>
        public override string ToString()
        {
            string shaderName = Shader != null ? Shader.name : "<null>";
            return $"ComputePassMeta: Shader='{shaderName}', KernelIndex={KernelIndex}, " +
                   $"ThreadGroups=({ThreadGroupsX}, {ThreadGroupsY}, {ThreadGroupsZ})";
        }
    }
}
