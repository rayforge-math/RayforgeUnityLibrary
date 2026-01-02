using System.Collections.Generic;
using Rayforge.ManagedResources.Abstractions;
using Rayforge.Diagnostics;

namespace Rayforge.Utility.Filter
{
    /// <summary>
    /// Represents a parametrized 1D filter function.
    /// Acts as a lightweight function object (functor) with explicit parameters.
    /// </summary>
    /// <typeparam name="Tparam">Filter-specific parameter type.</typeparam>
    public struct Filter<Tparam>
    {
        /// <summary>
        /// Delegate used to compute a kernel weight at a given radius index.
        /// </summary>
        /// <param name="x">Distance from the kernel center.</param>
        /// <param name="param">Filter-specific parameter.</param>
        /// <returns>Computed kernel weight.</returns>
        public delegate float FilterFunction(int x, Tparam param);

        private FilterFunction m_FilterFunc;
        private Tparam m_Param;

        /// <summary>
        /// Parameter passed to the filter function during evaluation.
        /// </summary>
        public Tparam Param
        {
            get => m_Param;
            set => m_Param = value;
        }

        /// <summary>
        /// Creates a new filter wrapper around the given function and parameter.
        /// </summary>
        public Filter(FilterFunction function, Tparam param)
        {
            m_FilterFunc = function;
            m_Param = param;
        }

        /// <summary>
        /// Evaluates the filter at the given kernel index.
        /// </summary>
        public float Invoke(int x)
            => m_FilterFunc.Invoke(x, m_Param);
    }

    /// <summary>
    /// Represents a symmetric, optionally normalized 1D convolution kernel.
    /// The kernel stores only the non-negative half [0..radius], assuming symmetry.
    /// </summary>
    public struct FilterKernel : IComputeDataArray<float>
    {
        private float[] m_Kernel;
        private bool m_Changed;
        private int m_Radius;

        /// <summary>
        /// Read-only view of the kernel coefficients.
        /// Index 0 corresponds to the kernel center.
        /// </summary>
        public IReadOnlyList<float> Kernel => m_Kernel;

        /// <summary>
        /// Raw float array for GPU buffer uploads.
        /// </summary>
        public float[] RawData => m_Kernel;

        /// <summary>
        /// Number of elements in the kernel buffer.
        /// </summary>
        public int Count => m_Kernel?.Length ?? 0;

        /// <summary>
        /// Direct access to kernel coefficients.
        /// </summary>
        public float this[int index] => m_Kernel[index];

        /// <summary>
        /// Kernel radius.
        /// Changing this marks the kernel for recomputation.
        /// </summary>
        public int Radius
        {
            get => m_Radius;
            set
            {
                if (m_Radius != value)
                {
                    Assertions.AtLeastZero(value, "radius must be greater than or equal to 0");
                    m_Radius = value;
                    m_Changed = true;
                }
            }
        }

        /// <summary>
        /// Converts a kernel radius to the required buffer size.
        /// </summary>
        public static int ToBufferSize(int radius) => radius + 1;

        /// <summary>
        /// Creates a new kernel with the given radius.
        /// </summary>
        public FilterKernel(int radius)
        {
            Assertions.AtLeastZero(radius, "radius must be greater than or equal to 0");
            m_Radius = radius;
            m_Changed = false;
            m_Kernel = new float[ToBufferSize(radius)];
        }

        /// <summary>
        /// Recomputes the kernel values using the provided filter.
        /// Must be called before sampling or uploading the kernel.
        /// </summary>
        /// <remarks>
        /// Kernel values are always recomputed, as filter parameters may change
        /// independently of the radius.
        /// </remarks>
        public void Apply<Tparam>(Filter<Tparam> filter, bool normalize = true)
        {
            if (m_Changed)
            {
                m_Kernel = new float[ToBufferSize(m_Radius)];
                m_Changed = false;
            }

            float sum = 0f;

            for (int i = 0; i <= m_Radius; ++i)
            {
                float value = filter.Invoke(i);
                m_Kernel[i] = value;

                sum += (i == 0) ? value : value * 2f;
            }

            if (normalize && sum > 0f)
            {
                for (int i = 0; i <= m_Radius; ++i)
                    m_Kernel[i] /= sum;
            }
        }
    }
}