using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

using Rayforge.Maths.Spaces;

using static Unity.Mathematics.math;

namespace Rayforge.Maths.Tranforms
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
        /// If true, performs the inverse FFT (IFFT); otherwise, performs the forward FFT.
        /// The IFFT result is unnormalized, so the output is scaled by the array length.
        /// </summary>
        public bool _FftInverse;

        /// <summary>
        /// If true, the result of the IFFT will be normalized (each element divided by the array length).
        /// Ignored for forward FFT.
        /// </summary>
        public bool _FftNormalize;

        /// <summary>
        /// Executes the FFT on the provided <see cref="_Samples"/> array.
        /// Calls <see cref="FFT()"/> internally.
        /// </summary>
        public void Execute()
        {
            FFT(0);
        }

        /// <summary>
        /// Retrieves a complex sample from the internal working buffer.
        /// </summary>
        /// <param name="baseOffset">Buffer segment base offset, for HLSL implementation.</param>
        /// <param name="index">Zero-based index of the sample to read.</param>
        /// <returns>The complex value stored at the specified index.</returns>
        private Complex GetSample(int baseOffset, int index)
            => _Samples[index];

        /// <summary>
        /// Writes a complex sample into the internal working buffer.
        /// </summary>
        /// <param name="baseOffset">Buffer segment base offset, for HLSL implementation.</param>
        /// <param name="index">Zero-based index of the sample to modify.</param>
        /// <param name="sample">The complex value to assign.</param>
        private void SetSample(int baseOffset, int index, Complex sample)
            => _Samples[index] = sample;

        /// <summary>
        /// Returns the total number of complex samples stored
        /// in the internal working buffer.
        /// </summary>
        /// <returns>The number of elements in the sample array.</returns>
        private int GetLength()
            => _Samples.Length;

        // --- HLSL-Bridge ---
        // (Functions in this region make hlsl-style code compatible with C# functionality)
        #region HLSL-Bridge

        /// <summary>
        /// Computes the twiddle factor e^{i·angle} for the FFT, using Euler's identity (see <see href="https://en.wikipedia.org/wiki/Euler%27s_identity" />)
        /// </summary>
        /// <param name="angle">Angle in radians.</param>
        /// <returns>Unit-magnitude complex number.</returns>
        private static Complex TwiddleFactor(float angle)
            => new Polar(1.0f, angle).ToComplex();

        /// <summary>
        /// Complex multiplication (matches HLSL implementation).
        /// </summary>
        /// <returns>Complex product.</returns>
        private static Complex ComplexMul(Complex lhs, Complex rhs)
            => lhs * rhs;

        /// <summary>
        /// Complex addition (matches HLSL implementation).
        /// </summary>
        /// <returns>Sum of two complex numbers.</returns>
        private static Complex ComplexAdd(Complex lhs, Complex rhs)
            => lhs + rhs;

        /// <summary>
        /// Complex subtraction (matches HLSL implementation).
        /// </summary>
        /// <returns>Difference of two complex numbers.</returns>
        private static Complex ComplexSub(Complex lhs, Complex rhs)
            => lhs - rhs;

        /// <summary>
        /// Scales a complex number by a scalar (matches HLSL implementation).
        /// </summary>
        /// <returns>Scaled complex number.</returns>
        private static Complex ComplexScale(Complex lhs, float rhs)
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
        int lg2(int N)
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
        int BitReverse(int x, int log2n)
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
        /// Reorders the elements in the buffer segment using bit-reversal ordering.
        /// e.g. 0, 1, 2, 3, 4, 5, 6, 7 -> 0, 4, 1, 5, 2, 6, 3, 7
        /// </summary>
        /// <param name="baseOffset">Offset into the buffer for the segment.</param>
        /// <param name="N">Number of elements in the segment.</param>
        /// <param name="bits">Number of bits used for indexing (log2(N)).</param>
        void BitReversal(int baseOffset, int N, int bits)
        {
            for (int i = 0; i < N; ++i)
            {
                int j = BitReverse(i, bits);
                if (i < j)
                {
                    Complex tmp = GetSample(baseOffset, i);
                    SetSample(baseOffset, i, GetSample(baseOffset, j));
                    SetSample(baseOffset, j, tmp);
                }
            }
        }

        /// <summary>
        /// Performs a DFT on a sub-segment of the FFT.
        /// </summary>
        /// <param name="baseOffset">Offset of the FFT segment within the buffer.</param>
        /// <param name="k">Start index of the current block within the segment.</param>
        /// <param name="m2">Half-length of the current sub-FFT (number of butterfly pairs).</param>
        /// <param name="theta">Twiddle angle factor for the current stage.</param>
        void DFT(int baseOffset, int k, int m2, float theta)
        {
            for (int j = 0; j < m2; ++j)
            {
                float ang = theta * j;
                Complex w = TwiddleFactor(ang);

                Complex p = GetSample(baseOffset, k + j);
                Complex q = ComplexMul(w, GetSample(baseOffset, k + j + m2));

                SetSample(baseOffset, k + j, ComplexAdd(p, q));
                SetSample(baseOffset, k + j + m2, ComplexSub(p, q));
            }
        }


        /// <summary>
        /// Performs a single FFT stage (butterfly computations) for a given sub-FFT length.
        /// </summary>
        /// <param name="baseOffset">Offset into the buffer for the segment.</param>
        /// <param name="N">Total length of the FFT segment.</param>
        /// <param name="m">Length of the current sub-FFT (m).</param>
        /// <param name="theta">Twiddle angle factor for this stage.</param>
        void FFTStage(int baseOffset, int N, int m, float theta)
        {
            int m2 = m >> 1;                                            // half-length

            for (int k = 0; k < N; k += m)
            {
                DFT(baseOffset, k, m2, theta);
            }
        }

        /// <summary>
        /// Performs the iterative Cooley-Tukey radix-2 FFT computation on the buffer segment.
        /// </summary>
        /// <param name="baseOffset">Offset into the buffer for the segment.</param>
        /// <param name="N">Number of elements in the segment.</param>
        /// <param name="bits">Number of bits used for indexing (log2(N)).</param>
        void Radix2FFT(int baseOffset, int N, int bits)
        {
            for (int s = 1; s <= bits; ++s)
            {
                int m = 1 << s;                                             // current sub-FFT length
                float theta = (_FftInverse ? 1.0f : -1.0f) * 2.0f * PI / m; // twiddle angle

                FFTStage(baseOffset, N, m, theta);
            }
        }

        /// <summary>
        /// Normalizes the buffer segment if performing an inverse FFT.
        /// </summary>
        /// <param name="baseOffset">Offset into the buffer for the segment.</param>
        /// <param name="N">Number of elements in the segment.</param>
        void NormalizeIfInverse(int baseOffset, int N)
        {
            if (!_FftInverse || !_FftNormalize) return;

            float invN = 1.0f / N;
            for (int i = 0; i < N; ++i)
            {
                SetSample(baseOffset, i, ComplexScale(GetSample(baseOffset, i), invN));
            }
        }

        /// <summary>
        /// Performs an in-place iterative Fast Fourier Transform (FFT) or inverse FFT (IFFT)
        /// on a segment of a <see cref="NativeArray{Complex}"/> identified by <paramref name="baseOffset"/>.
        /// Implements the Cooley-Tukey radix-2 algorithm.
        /// </summary>
        /// <param name="baseOffset">Offset into the buffer for the segment to transform.</param>
        [BurstCompile]
        void FFT(int baseOffset)
        {
            int N = GetLength();
            int bits = lg2(N);

            BitReversal(baseOffset, N, bits);
            Radix2FFT(baseOffset, N, bits);
            NormalizeIfInverse(baseOffset, N);
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
        /// Executes the normalization on the <see cref="_Samples"/> array.
        /// Each element is divided by the length of the array.
        /// </summary>
        public void Execute()
        {
            NormalizeFFT(0);
        }

        /// <summary>
        /// Retrieves a complex sample from the internal working buffer.
        /// </summary>
        /// <param name="baseOffset">Buffer segment base offset, for HLSL implementation.</param>
        /// <param name="index">Zero-based index of the sample to read.</param>
        /// <returns>The complex value stored at the specified index.</returns>
        private Complex GetSample(int baseOffset, int index)
            => _Samples[index];

        /// <summary>
        /// Writes a complex sample into the internal working buffer.
        /// </summary>
        /// <param name="baseOffset">Buffer segment base offset, for HLSL implementation.</param>
        /// <param name="index">Zero-based index of the sample to modify.</param>
        /// <param name="sample">The complex value to assign.</param>
        private void SetSample(int baseOffset, int index, Complex sample)
            => _Samples[index] = sample;

        /// <summary>
        /// Returns the total number of complex samples stored
        /// in the internal working buffer.
        /// </summary>
        /// <returns>The number of elements in the sample array.</returns>
        private int GetLength()
            => _Samples.Length;

        // --- HLSL-Bridge ---
        // (Functions in this region make hlsl-style code compatible with C# functionality)
        #region HLSL-Bridge

        /// <summary>
        /// Scales a complex number by a scalar (matches HLSL implementation).
        /// </summary>
        /// <returns>Scaled complex number.</returns>
        private static Complex ComplexScale(Complex lhs, float rhs)
            => lhs * rhs;

        #endregion

        // --- HLSL-Compatible FFT Normalize ---
        // (Functions in this region mirror their HLSL equivalents)
        #region HLSL-Compatible

        /// <summary>
        /// Normalizes a NativeArray of complex numbers by dividing each element by number of elements.
        /// Useful after an unnormalized inverse FFT (IFFT).
        /// </summary>
        [BurstCompile]
        void NormalizeFFT(int baseOffset)
        {
            float invN = 1.0f / GetLength();

            for (int i = 0; i < GetLength(); ++i)
            {
                SetSample(baseOffset, i, ComplexScale(GetSample(baseOffset, i), invN));
            }
        }

        #endregion // --- HLSL-Compatible FFT Normalize ---
    }

    /// <summary>
    /// Job that performs element-wise complex multiplication between two
    /// frequency-domain signals (i.e., frequency-domain convolution).
    /// Convolution in the frequency domain is commutative, which holds for n-dimensional separable transforms as well.
    /// </summary>
    /// <remarks>
    /// This job assumes that both <see cref="_Samples"/> and <see cref="_Filter"/>:
    /// <list type="bullet">
    /// <item><description>are the same length</description></item>
    /// <item><description>represent forward FFT output</description></item>
    /// <item><description>are already aligned for convolution (e.g., zero-padded to avoid circular convolution artifacts)</description></item>
    /// </list>
    /// <para>
    /// The job performs pointwise multiplication:
    /// <c>H[k] = X[k] * W[k]</c>,
    /// which corresponds to convolution in the time domain.
    /// </para>
    /// </remarks>
    [BurstCompile]
    public struct FrequencyConvolutionJob : IJob
    {
        /// <summary>
        /// The target frequency-domain samples to be modified in place.
        /// This buffer will hold the convolution result.
        /// </summary>
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Complex> _Samples;

        /// <summary>
        /// The frequency-domain filter kernel.
        /// Must have the same length as <see cref="_Samples"/>.
        /// </summary>
        [ReadOnly]
        public NativeArray<Complex> _Filter;

        /// <summary>
        /// Executes the frequency-domain convolution job.
        /// Performs element-wise complex multiplication and optional normalization.
        /// </summary>
        public void Execute()
        {
            Convolute(0);
        }

        /// <summary>
        /// Retrieves a complex sample from the internal working buffer.
        /// </summary>
        /// <param name="baseOffset">Buffer segment base offset, for HLSL implementation.</param>
        /// <param name="index">Zero-based index of the sample to read.</param>
        /// <returns>The complex value stored at the specified index.</returns>
        private Complex GetSample(int baseOffset, int index)
            => _Samples[index];

        /// <summary>
        /// Writes a complex sample into the internal working buffer.
        /// </summary>
        /// <param name="baseOffset">Buffer segment base offset, for HLSL implementation.</param>
        /// <param name="index">Zero-based index of the sample to modify.</param>
        /// <param name="sample">The complex value to assign.</param>
        private void SetSample(int baseOffset, int index, Complex sample)
            => _Samples[index] = sample;

        /// <summary>
        /// Returns the total number of complex samples stored
        /// in the internal working buffer.
        /// </summary>
        /// <returns>The number of elements in the sample array.</returns>
        private int GetLength()
            => _Samples.Length;

        /// <summary>
        /// Retrieves a complex filter sample from the internal filter buffer.
        /// </summary>
        /// <param name="index">Zero-based index of the sample to read.</param>
        /// <returns>The complex value stored at the specified index.</returns>
        private Complex GetFilter(int index)
            => _Filter[index];

        // --- HLSL-Bridge ---
        // (Functions in this region make hlsl-style code compatible with C# functionality)
        #region HLSL-Bridge

        /// <summary>
        /// Complex multiplication (matches HLSL implementation).
        /// </summary>
        /// <param name="lhs">Left operand.</param>
        /// <param name="rhs">Right operand.</param>
        /// <returns>Complex product.</returns>
        private static Complex ComplexMul(Complex lhs, Complex rhs)
            => lhs * rhs;

        #endregion

        // --- HLSL-Compatible Frequency Domain Convolution ---
        // (Functions in this region mirror their HLSL equivalents)
        #region HLSL-Compatible

        /// <summary>
        /// Performs pointwise complex multiplication:
        /// S[k] = S[k] * F[k]
        /// </summary>
        [BurstCompile]
        void Convolute(int baseOffset)
        {
            for (int i = 0; i < GetLength(); ++i)
            {
                SetSample(baseOffset, i, ComplexMul(GetSample(baseOffset, i), GetFilter(i)));
            }
        }

        #endregion // --- HLSL-Compatible Frequency Domain Convolution ---
    }
}
