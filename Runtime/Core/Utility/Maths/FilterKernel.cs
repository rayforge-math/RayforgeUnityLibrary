using UnityEngine;
using System.Collections.Generic;
using Rayforge.ManagedResources.NativeMemory;

using static Rayforge.Utility.RuntimeCheck.Asserts;

namespace Rayforge.Utility.Maths.Filter
{
    public static class GaussHelpers
    {
        /// <summary>
        /// Computes the value of a 1D Gaussian function at integer offset x.
        /// Useful for generating blur kernels and falloff curves.
        /// </summary>
        /// <param name="x">The sample offset from the center (0).</param>
        /// <param name="sigma">Standard deviation controlling the spread of the Gaussian.</param>
        /// <returns>The unnormalized Gaussian weight at position x.</returns>
        public static float Gaussian1D(int x, float sigma)
        {
            return Mathf.Exp(-(x * x) / (2.0f * sigma * sigma));
        }

        /// <summary>
        /// Computes the Gaussian sigma value given a kernel index x and weight y.
        /// Useful when reconstructing sigma from curve points.
        /// </summary>
        /// <param name="x">Sample index.</param>
        /// <param name="y">Amplitude at that index.</param>
        /// <returns>The reconstructed sigma.</returns>
        public static float Gaussian1DSigma(int x, float y)
        {
            return Mathf.Sqrt(-0.5f * Mathf.Pow(x, 2) / Mathf.Log(y));
        }
    }

    /// <summary>
    /// Represents a normalized symmetric 1D convolution kernel, typically used in blur filters (e.g., Gaussian blur),
    /// parameterized by <typeparamref name="Tparam"/> which defines the type of additional filter parameters.
    /// </summary>
    /// <typeparam name="Tparam">Type used to provide filter-specific parameters to the <see cref="FilterFunction"/>.</typeparam>
    public struct FilterKernel<Tparam> : IComputeDataArray<float>
    {
        private float[] m_Kernel;

        /// <summary>
        /// The computed kernel values, symmetric around index 0.
        /// </summary>
        public IReadOnlyList<float> Kernel => m_Kernel;

        /// <summary>
        /// Raw float array used for GPU buffer uploads.
        /// </summary>
        public readonly float[] RawData => m_Kernel;

        /// <summary>
        /// Number of elements in the raw float array used for GPU buffer uploads.
        /// </summary>
        public int Count => m_Kernel?.Length ?? 0;

        /// <summary>
        /// Provides direct access to the kernel coefficients.
        /// </summary>
        /// <param name="index">Index in the kernel array.</param>
        public float this[int index] => m_Kernel[index];

        private readonly int k_KernelRadiusMax;

        /// <summary>
        /// Function used to compute a kernel weight at a given radius index,
        /// using a parameter of type <typeparamref name="Tparam"/>.
        /// </summary>
        /// <param name="radius">The distance from the center of the kernel.</param>
        /// <param name="param">Filter-specific parameter of type <typeparamref name="Tparam"/>.</param>
        /// <returns>Computed kernel weight for the given radius.</returns>
        public delegate float FilterFunction(int radius, Tparam param);
        private FilterFunction m_FilterFunc;

        /// <summary>
        /// Function used to compute a <typeparamref name="Tparam"/> instance based on the current radius.
        /// </summary>
        /// <param name="radius">Current kernel radius.</param>
        /// <returns>Parameter of type <typeparamref name="Tparam"/> to be passed into <see cref="FilterFunction"/>.</returns>
        public delegate Tparam ParamFunction(int radius);
        private ParamFunction m_ParamFunc;

        private bool m_Changed;
        private int m_Radius;

        /// <summary>
        /// The active kernel radius. Controls the number of computed samples.
        /// Setting this triggers kernel recomputation on the next <see cref="Apply"/>.
        /// </summary>
        public int Radius
        {
            get => m_Radius;
            set
            {
                if (m_Radius != value)
                {
                    ValidateRadius(value, k_KernelRadiusMax);
                    m_Radius = value;
                    m_Changed = true;
                }
            }
        }

        /// <summary>
        /// Converts a kernel radius to the backing array size.
        /// </summary>
        /// <param name="radius">Kernel radius to convert.</param>
        /// <returns>Array size required to store the kernel values for the given radius.</returns>
        public static int ToBufferSize(int radius) => radius + 1;

        /// <summary>
        /// Current size of the kernel buffer based on the active radius.
        /// </summary>
        public int BufferSize => ToBufferSize(m_Radius);

        /// <summary>
        /// Creates a new filter kernel using the provided filter and parameter functions.
        /// </summary>
        /// <param name="filterFunc">Function mapping (index, <typeparamref name="Tparam"/>) to kernel weight.</param>
        /// <param name="paramFunc">Function computing the filter parameter of type <typeparamref name="Tparam"/> based on radius.</param>
        /// <param name="radiusMax">Maximum allowed kernel radius.</param>
        public FilterKernel(FilterFunction filterFunc, ParamFunction paramFunc, int radiusMax)
        {
            ValidateDelegate(filterFunc);
            ValidateDelegate(paramFunc);
            ValidateRadius(radiusMax, radiusMax);

            m_FilterFunc = filterFunc;
            m_ParamFunc = paramFunc;
            k_KernelRadiusMax = radiusMax;

            m_Kernel = new float[k_KernelRadiusMax + 1];
            m_Radius = k_KernelRadiusMax;
            m_Changed = true;
        }

        /// <summary>
        /// Recomputes and normalizes the kernel if any parameter has changed.
        /// Call this before sampling or uploading to the GPU.
        /// </summary>
        public void Apply()
        {
            if (m_Changed)
            {
                Tparam param = m_ParamFunc(m_Radius);

                float sum = 0f;
                for (int i = 0; i <= m_Radius; ++i)
                {
                    float value = m_FilterFunc(i, param);
                    m_Kernel[i] = value;
                    sum += value * 2;
                }

                for (int i = 0; i <= m_Radius; ++i)
                    m_Kernel[i] /= sum;

                m_Changed = false;
            }
        }

        /// <summary>
        /// Ensures the provided radius is within valid bounds.
        /// </summary>
        /// <param name="radius">Radius to validate.</param>
        /// <param name="radiusMax">Maximum allowed radius.</param>
        private static void ValidateRadius(int radius, int radiusMax)
        {
            string error = $"radius must be x where 0 <= x && x <= {radiusMax}";
            Validate(radius, val => 0 <= radius && radius <= radiusMax, error);
        }
    }
}