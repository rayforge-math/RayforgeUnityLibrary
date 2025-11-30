using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

using Rayforge.ManagedResources.NativeMemory;

using static Unity.Mathematics.math;

namespace Rayforge.Utility.Maths.FFT
{
    /// <summary>
    /// Fast Fourier Transform implementation for arrays of Complex numbers (power-of-2 length).
    /// Uses in-place iterative Cooley-Tukey algorithm with bit reversal.
    /// </summary>
    [BurstCompile]
    public static class FFTUtility
    {
        /// <summary>
        /// Computes the integer base-2 logarithm of N (assumes N = 2^n, where n∈N).
        /// </summary>
        /// <param name="N">Length of the array (power of 2).</param>
        /// <returns>Number of bits required to represent indices.</returns>
        private static int lg2(int N)
        {
            int count = 0;

            N >>= 1;
            while (N != 0)
            {
                N >>= 1;
                count++;
            }

            return count;
        }

        /// <summary>
        /// Computes the bit-reversed index for a given index x.
        /// </summary>
        /// <param name="x">Original index.</param>
        /// <param name="log2n">Number of bits.</param>
        /// <returns>Bit-reversed index.</returns>
        private static int BitReverse(int x, int log2n)
        {
            int n = 0;
            for (int i = 0; i < log2n; i++)
            {
                n <<= 1;
                n |= (x & 1);
                x >>= 1;
            }
            return n;
        }

        /// <summary>
        /// Swaps two elements in the array.
        /// </summary>
        /// <param name="samples">Array of Complex numbers.</param>
        /// <param name="i">Index of first element.</param>
        /// <param name="j">Index of second element.</param>
        private static void Swap(ref NativeArray<Complex> samples, int i, int j)
        {
            var tmp = samples[i];
            samples[i] = samples[j];
            samples[j] = tmp;
        }

        /// <summary>
        /// Performs in-place bit reversal on the array to prepare for FFT, 
        /// e.g. 0, 1, 2, 3, 4, 5, 6, 7 -> 0, 4, 1, 5, 2, 6, 3, 7
        /// </summary>
        /// <param name="samples">Array of Complex numbers.</param>
        private static void BitReversalInPlace(ref NativeArray<Complex> samples)
        {
            int N = samples.Length;
            int bits = lg2(N);
            for (int i = 0; i < N; ++i)
            {
                int j = BitReverse(i, bits);
                if (i < j) Swap(ref samples, i, j);
            }
        }

        /// <summary>
        /// Normalizes a NativeArray of complex numbers by dividing each element by a scalar.
        /// Useful after an unnormalized inverse FFT (IFFT).
        /// </summary>
        /// <param name="samples">Array of <see cref="Complex"/> numbers to normalize.</param>
        private static void Normalize(ref NativeArray<Complex> samples)
        {
            float invN = 1.0f / samples.Length;
            for (int i = 0; i < samples.Length; ++i)
            {
                samples[i] *= invN;
            }
        }

        /// <summary>
        /// Convenience method to normalize an IFFT result by array length.
        /// </summary>
        /// <param name="samples">Array of <see cref="Complex"/> numbers.</param>
        [BurstCompile]
        public static void NormalizeIFFT(ref NativeArray<Complex> samples)
        {
            Normalize(ref samples);
        }

        /// <summary>
        /// Performs an in-place iterative Fast Fourier Transform (FFT) or inverse FFT (IFFT) on a <see cref="NativeArray{Complex}"/>.
        /// Implements the Cooley-Tukey radix-2 algorithm with bit-reversal ordering.
        /// </summary>
        /// <param name="samples">
        /// Reference to the <see cref="NativeArray{Complex}"/> containing the complex numbers to transform. 
        /// The length of the array must be a power of 2. The array is modified in place.
        /// </param>
        /// <param name="inverse">
        /// If true, performs the inverse FFT (IFFT); otherwise, performs the forward FFT.
        /// The IFFT is unnormalized by default, meaning the output is scaled by N (the array length).
        /// </param>
        /// <param name="normalize">
        /// If true and <paramref name="inverse"/> is also true, scales each element by 1/N after the IFFT
        /// to recover the original signal amplitude.
        /// </param>
        [BurstCompile]
        public static void FFT(ref NativeArray<Complex> samples, bool inverse = false, bool normalize = false)
        {
            BitReversalInPlace(ref samples);

            int N = samples.Length;
            int bits = lg2(N);

            for (int s = 1; s <= bits; ++s)
            {
                int m = 1 << s;                                         // current sub-FFT length
                int m2 = m >> 1;                                        // half-length
                float theta = (inverse ? 1.0f : -1.0f) * 2.0f * PI / m; // twiddle angle

                for (int k = 0; k < N; k += m)                          // iterate over blocks
                {
                    for (int j = 0; j < m2; ++j)                        // iterate over block elements
                    {
                        float ang = theta * j;
                        Complex w = new Polar(1.0f, ang).ToComplex();   // twiddle factor

                        Complex p = samples[k + j];
                        Complex q = w * samples[k + j + m2];

                        samples[k + j] = p + q;
                        samples[k + j + m2] = p - q;
                    }
                }
            }

            if(inverse && normalize)
            {
                Normalize(ref samples);
            }
        }
    }

    /// <summary>
    /// A Unity Job that performs an in-place FFT or inverse FFT on a NativeArray of Complex numbers.
    /// This job can be scheduled using the Unity Job System and is compatible with Burst compilation.
    /// </summary>
    [BurstCompile]
    public struct FFTJob : IJob
    {
        /// <summary>
        /// The array of complex numbers to transform.
        /// Must have a length that is a power of 2.
        /// </summary>
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Complex> _Samples;

        /// <summary>
        /// If true, performs the inverse FFT (IFFT); otherwise, performs the forward FFT.
        /// The IFFT result is unnormalized, so the output is scaled by the array length.
        /// </summary>
        public bool _Inverse;

        /// <summary>
        /// If true, the result of the IFFT will be normalized (each element divided by the array length).
        /// Ignored for forward FFT.
        /// </summary>
        public bool _Normalize;

        /// <summary>
        /// Executes the FFT on the provided <see cref="_Samples"/> array.
        /// Calls <see cref="FFTUtility.FFT(NativeArray{Complex}, bool, bool)"/> internally.
        /// </summary>
        public void Execute()
        {
            FFTUtility.FFT(ref _Samples, _Inverse, _Normalize);
        }
    }

    /// <summary>
    /// A Unity Job that normalizes a <see cref="NativeArray{Complex}"/> in place.
    /// Each element is scaled by 1/N, where N is the length of the array.
    /// This is useful for normalizing the output of an inverse FFT (IFFT).
    /// </summary>
    [BurstCompile]
    public struct FFTNormalizeJob : IJob
    {
        /// <summary>
        /// The array of complex numbers to normalize.
        /// </summary>
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Complex> Samples;

        /// <summary>
        /// Executes the normalization on the <see cref="Samples"/> array.
        /// Each element is divided by the length of the array.
        /// </summary>
        public void Execute()
        {
            FFTUtility.NormalizeIFFT(ref Samples);
        }
    }
}
