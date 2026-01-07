using Rayforge.Diagnostics;
using UnityEngine;

namespace Rayforge.Rendering.Passes
{
    /// <summary>
    /// Metadata for a compute pass, including the kernel and dispatch thread group sizes.
    /// </summary>
    public struct ComputeDispatchMeta
    {
        /// <summary>
        /// Metadata describing the kernel (shader and kernel index).
        /// </summary>
        public readonly ComputeKernelMeta KernelMeta;

        private int _threadGroupsX;
        private int _threadGroupsY;
        private int _threadGroupsZ;

        /// <summary>
        /// Number of thread groups in X dimension.
        /// </summary>
        public int ThreadGroupsX
        {
            get => _threadGroupsX;
            set
            {
                Assertions.AtLeastZero(value);
                _threadGroupsX = value;
            }
        }

        /// <summary>
        /// Number of thread groups in Y dimension.
        /// </summary>
        public int ThreadGroupsY
        {
            get => _threadGroupsY;
            set
            {
                Assertions.AtLeastZero(value);
                _threadGroupsY = value;
            }
        }

        /// <summary>
        /// Number of thread groups in Z dimension.
        /// </summary>
        public int ThreadGroupsZ
        {
            get => _threadGroupsZ;
            set
            {
                Assertions.AtLeastZero(value);
                _threadGroupsZ = value;
            }
        }

        /// <summary>
        /// The compute shader used for this dispatch.
        /// </summary>
        public ComputeShader Shader => KernelMeta.Shader;

        /// <summary>
        /// The kernel index used for this dispatch.
        /// </summary>
        public int KernelIndex => KernelMeta.KernelIndex;

        /// <summary>
        /// Constructs a new <see cref="ComputePassMeta"/> from a kernel meta and thread group counts.
        /// </summary>
        /// <param name="kernelMeta">The kernel metadata (shader + kernel index).</param>
        /// <param name="threadGroupsX">Number of thread groups in X dimension.</param>
        /// <param name="threadGroupsY">Number of thread groups in Y dimension.</param>
        /// <param name="threadGroupsZ">Number of thread groups in Z dimension.</param>
        public ComputeDispatchMeta(ComputeKernelMeta kernelMeta, int threadGroupsX, int threadGroupsY, int threadGroupsZ)
        {
            KernelMeta = kernelMeta;

            Assertions.AtLeastZero(threadGroupsX);
            Assertions.AtLeastZero(threadGroupsY);
            Assertions.AtLeastZero(threadGroupsZ);

            _threadGroupsX = threadGroupsX;
            _threadGroupsY = threadGroupsY;
            _threadGroupsZ = threadGroupsZ;
        }

        /// <summary>
        /// Constructs a new <see cref="ComputePassMeta"/> from a shader, kernel name, and thread group counts.
        /// </summary>
        /// <param name="shader">The compute shader asset.</param>
        /// <param name="kernelName">The kernel name to resolve.</param>
        /// <param name="threadGroupsX">Number of thread groups in X dimension.</param>
        /// <param name="threadGroupsY">Number of thread groups in Y dimension.</param>
        /// <param name="threadGroupsZ">Number of thread groups in Z dimension.</param>
        public ComputeDispatchMeta(ComputeShader shader, string kernelName, int threadGroupsX, int threadGroupsY, int threadGroupsZ)
            : this(new ComputeKernelMeta(shader, kernelName), threadGroupsX, threadGroupsY, threadGroupsZ)
        { }

        /// <summary>
        /// Constructs a new <see cref="ComputePassMeta"/> from a shader, kernel index, and thread group counts.
        /// </summary>
        /// <param name="shader">The compute shader asset.</param>
        /// <param name="kernelIndex">The kernel index to use.</param>
        /// <param name="threadGroupsX">Number of thread groups in X dimension.</param>
        /// <param name="threadGroupsY">Number of thread groups in Y dimension.</param>
        /// <param name="threadGroupsZ">Number of thread groups in Z dimension.</param>
        public ComputeDispatchMeta(ComputeShader shader, int kernelIndex, int threadGroupsX, int threadGroupsY, int threadGroupsZ)
            : this(new ComputeKernelMeta(shader, kernelIndex), threadGroupsX, threadGroupsY, threadGroupsZ)
        { }

        /// <summary>
        /// Returns true if the kernel meta is valid and thread group counts are all non-negative.
        /// </summary>
        public bool IsValid => KernelMeta.IsValid &&
                               ThreadGroupsX >= 0 &&
                               ThreadGroupsY >= 0 &&
                               ThreadGroupsZ >= 0;
    }
}
