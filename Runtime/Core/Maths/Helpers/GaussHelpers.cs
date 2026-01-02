using UnityEngine;

namespace Rayforge.Utility.Maths.Helpers
{
    public static class GaussHelpers
    {
        /// <summary>
        /// Computes the value of an unnormalized 1D Gaussian function at integer offset x.
        /// Commonly used for blur kernels and smooth falloff curves.
        /// </summary>
        /// <param name="x">Sample offset from the kernel center (0).</param>
        /// <param name="sigma">Standard deviation controlling the width of the Gaussian.</param>
        /// <returns>Unnormalized Gaussian weight at position x.</returns>
        public static float Gaussian1D(int x, float sigma)
        {
            return Mathf.Exp(-(x * x) / (2.0f * sigma * sigma));
        }

        /// <summary>
        /// Reconstructs the Gaussian sigma value from a sample index and weight.
        /// </summary>
        /// <remarks>
        /// Requires y in the open interval (0, 1) and x != 0.
        /// Intended for analytical reconstruction or curve fitting, not runtime filtering.
        /// </remarks>
        /// <param name="x">Sample offset from the center.</param>
        /// <param name="y">Gaussian weight at that offset.</param>
        /// <returns>The reconstructed sigma value.</returns>
        public static float Gaussian1DSigma(int x, float y)
        {
            return Mathf.Sqrt(-0.5f * (x * x) / Mathf.Log(y));
        }
    }
}