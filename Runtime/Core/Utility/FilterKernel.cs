using UnityEngine;
using System.Collections.Generic;
using Rayforge.ManagedResources.NativeMemory;

using static Rayforge.Utility.RuntimeCheck.Asserts;

namespace Rayforge.Utility.Filter
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
    /// Represents a normalized symmetric 1D convolution kernel,
    /// typically used in blur filters (e.g., Gaussian blur).
    /// </summary>
    public struct FilterKernel : IComputeData<float>
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
        /// Number of elements in raw float array used for GPU buffer uploads.
        /// </summary>
        public int Count => m_Kernel?.Length ?? 0;

        /// <summary>
        /// Provides direct access to the kernel coefficients.
        /// </summary>
        public float this[int index] => m_Kernel[index];

        private readonly int k_KernelRadiusMax;

        /// <summary>
        /// Function used to compute a kernel weight at a given radius index.
        /// </summary>
        public delegate float FilterFunction(int radius, float param);
        private FilterFunction m_FilterFunc;

        /// <summary>
        /// Function used to compute filter parameters based on the current radius.
        /// </summary>
        public delegate float ParamFunction(int radius);
        private ParamFunction m_ParamFunc;

        private bool m_Changed;

        private int m_Radius;

        /// <summary>
        /// The active kernel radius. This controls the number of computed samples.
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
        public static int ToBufferSize(int radius) => radius + 1;

        /// <summary>
        /// Current size of the kernel buffer based on active radius.
        /// </summary>
        public int BufferSize => ToBufferSize(m_Radius);

        /// <summary>
        /// Creates a new filter kernel using the provided curve definition callbacks.
        /// </summary>
        /// <param name="filterFunc">Function mapping (index, parameter) to weight.</param>
        /// <param name="paramFunc">Function computing the curve parameter based on radius.</param>
        /// <param name="radiusMax">Maximum kernel radius allowed.</param>
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
        /// Call this before sampling or uploading to GPU.
        /// </summary>
        public void Apply()
        {
            if (m_Changed)
            {
                float param = m_ParamFunc(m_Radius);

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
        private static void ValidateRadius(int radius, int radiusMax)
        {
            string error = $"radius must be x where 0 <= x && x <= {radiusMax}";
            Validate(radius, val => 0 <= radius && radius <= radiusMax, error);
        }
    }
}