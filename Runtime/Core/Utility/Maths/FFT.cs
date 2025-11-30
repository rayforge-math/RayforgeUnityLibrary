using Rayforge.ManagedResources.NativeMemory;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.UIElements;
using static Unity.Mathematics.math;

namespace Rayforge.Utility.Maths.FFT
{
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
        /// Number of elements in array, internally used for HLSL compatibility.
        /// </summary>
        private int _Length;

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
        /// Calls <see cref="FFT()"/> internally.
        /// </summary>
        public void Execute()
        {
            _Length = _Samples.Length;
            FFT();
        }

        // --- HLSL-Bridge ---
        // (Functions in this region make hlsl-style code compatible with C# functionality)
        #region HLSL-Bridge

        /// <summary>
        /// Computes the twiddle factor e^{i·angle}.
        /// </summary>
        /// <param name="angle">Angle in radians.</param>
        /// <returns>Unit-magnitude complex number.</returns>
        private static Complex TwiddleFactor(float angle)
            => new Polar(1.0f, angle).ToComplex();

        /// <summary>
        /// Complex multiplication (matches HLSL implementation).
        /// </summary>
        /// <param name="lhs">Left operand.</param>
        /// <param name="rhs">Right operand.</param>
        /// <returns>Complex product.</returns>
        private static Complex ComplexMul(Complex lhs, Complex rhs)
            => lhs * rhs;

        #endregion

        // --- HLSL-Compatible FFT ---
        // (Functions in this region mirror their HLSL equivalents)
        #region HLSL-Compatible

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
        private int BitReverse(int x, int log2n)
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
        /// <param name="i">Index of first element.</param>
        /// <param name="j">Index of second element.</param>
        private void Swap(int i, int j)
        {
            var tmp = _Samples[i];
            _Samples[i] = _Samples[j];
            _Samples[j] = tmp;
        }

        /// <summary>
        /// Performs in-place bit reversal on the array to prepare for FFT, 
        /// e.g. 0, 1, 2, 3, 4, 5, 6, 7 -> 0, 4, 1, 5, 2, 6, 3, 7
        /// </summary>
        private void BitReversalInPlace()
        {
            int N = _Length;
            int bits = lg2(N);
            for (int i = 0; i < N; ++i)
            {
                int j = BitReverse(i, bits);
                if (i < j) Swap(i, j);
            }
        }

        /// <summary>
        /// Normalizes a NativeArray of complex numbers by dividing each element by number of elements.
        /// Useful after an unnormalized inverse FFT (IFFT).
        /// </summary>
        private void Normalize()
        {
            float invN = 1.0f / _Length;
            for (int i = 0; i < _Length; ++i)
            {
                _Samples[i] *= invN;
            }
        }

        /// <summary>
        /// Performs an in-place iterative Fast Fourier Transform (FFT) or inverse FFT (IFFT) on a <see cref="NativeArray{Complex}"/>.
        /// Implements the Cooley-Tukey radix-2 algorithm with bit-reversal ordering.
        /// </summary>
        [BurstCompile]
        private void FFT()
        {
            BitReversalInPlace();

            int N = _Length;
            int bits = lg2(N);

            for (int s = 1; s <= bits; ++s)
            {
                int m = 1 << s;                                             // current sub-FFT length
                int m2 = m >> 1;                                            // half-length
                float theta = (_Inverse ? 1.0f : -1.0f) * 2.0f * PI / m;    // twiddle angle

                for (int k = 0; k < N; k += m)                              // iterate over blocks
                {
                    for (int j = 0; j < m2; ++j)                            // iterate over block elements
                    {
                        float ang = theta * j;
                        Complex w = TwiddleFactor(ang);

                        Complex p = _Samples[k + j];
                        Complex q = ComplexMul(w, _Samples[k + j + m2]);

                        _Samples[k + j] = p + q;
                        _Samples[k + j + m2] = p - q;
                    }
                }
            }

            if (_Inverse && _Normalize)
            {
                Normalize();
            }
        }

        #endregion // --- HLSL-Compatible FFT ---
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
        public NativeArray<Complex> _Samples;

        /// <summary>
        /// Number of elements in array, internally used for HLSL compatibility.
        /// </summary>
        private int _Length;

        /// <summary>
        /// Executes the normalization on the <see cref="_Samples"/> array.
        /// Each element is divided by the length of the array.
        /// </summary>
        public void Execute()
        {
            _Length = _Samples.Length;
            Normalize();
        }

        // --- HLSL-Compatible FFT Normalize ---
        // (Functions in this region mirror their HLSL equivalents)
        #region HLSL-Compatible

        /// <summary>
        /// Normalizes a NativeArray of complex numbers by dividing each element by number of elements.
        /// Useful after an unnormalized inverse FFT (IFFT).
        /// </summary>
        [BurstCompile]
        private void Normalize()
        {
            float invN = 1.0f / _Length;
            for (int i = 0; i < _Length; ++i)
            {
                _Samples[i] *= invN;
            }
        }

        #endregion // --- HLSL-Compatible FFT Normalize ---
    }
}
