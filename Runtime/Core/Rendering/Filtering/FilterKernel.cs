using Rayforge.Core.Diagnostics;
using Rayforge.Core.ManagedResources.Abstractions;
using System;
using System.Collections.Generic;

namespace Rayforge.Core.Rendering.Filtering
{
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
        /// <exception cref="ArgumentOutOfRangeException">Thrown if radius is negative.</exception>
        public int Radius
        {
            get => m_Radius;
            set
            {
                if (m_Radius != value)
                {
                    CheckRadius(value);
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
        /// <exception cref="ArgumentOutOfRangeException">Thrown if radius is negative.</exception>
        public FilterKernel(int radius)
        {
            CheckRadius(radius);

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
        public void Apply<TParam>(Filter<TParam> filter, bool normalize = true)
        {
            if (m_Changed)
            {
                m_Kernel = new float[ToBufferSize(m_Radius)];
                m_Changed = false;
            }

            float sum = 0f;

            for (int i = 0; i < m_Kernel.Length; ++i)
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

        /// <summary>
        /// Throws an exception if the radius is negative.
        /// Ensures kernel integrity.
        /// </summary>
        /// <param name="radius">Radius to check.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if radius is negative.</exception>
        private static void CheckRadius(int radius)
        {
            if (radius < 0)
                throw new ArgumentOutOfRangeException(nameof(radius), "Radius cannot be negative.");
        }
    }
}