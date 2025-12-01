using Rayforge.ManagedResources.NativeMemory;
using Rayforge.Utility.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

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
        /// Computes the twiddle factor e^{i·angle} for the FFT, using euler's identity (see <see href="https://en.wikipedia.org/wiki/Euler%27s_identity" />)
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
        /// Performs an in-place iterative Fast Fourier Transform (FFT) or inverse FFT (IFFT) on a <see cref="NativeArray{Complex}"/>.
        /// Implements the Cooley-Tukey radix-2 algorithm with bit-reversal ordering.
        /// </summary>
        [BurstCompile]
        void FFT(int baseOffset)
        {
            int N = GetLength();
            int bits = lg2(N);

            // Performs in-place bit reversal on the array to prepare for FFT, 
            // e.g. 0, 1, 2, 3, 4, 5, 6, 7 -> 0, 4, 1, 5, 2, 6, 3, 7
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

            // Performs actual FFT
            for (int s = 1; s <= bits; ++s)
            {
                int m = 1 << s;                                             // current sub-FFT length
                int m2 = m >> 1;                                            // half-length
                float theta = (_FftInverse ? 1.0f : -1.0f) * 2.0f * PI / m; // twiddle angle

                for (int k = 0; k < N; k += m)                              // iterate over blocks
                {
                    for (int j = 0; j < m2; ++j)                            // iterate over block elements
                    {
                        float ang = theta * j;
                        Complex w = TwiddleFactor(ang);

                        Complex p = GetSample(baseOffset, k + j);
                        Complex q = ComplexMul(w, GetSample(baseOffset, k + j + m2));

                        SetSample(baseOffset, k + j, ComplexAdd(p, q));
                        SetSample(baseOffset, k + j + m2, ComplexSub(p, q));
                    }
                }
            }

            if (_FftInverse && _FftNormalize)
            {
                float invN = 1.0f / GetLength();
                for (int i = 0; i < GetLength(); ++i)
                {
                    SetSample(baseOffset, i, ComplexScale(GetSample(baseOffset, i), invN));
                }
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


    /// <summary>
    /// Dispatcher for scheduling and completing 1D FFT and IFFT jobs.
    /// </summary>
    /// <remarks>
    /// The provided wrapper methods are convenience helpers designed to simplify
    /// common FFT workflows and to serve as reference examples for how to use the
    /// underlying FFT job system.  
    /// <para>
    /// While these wrappers are suitable for general use, they may not always be
    /// the most optimal choice for performance–critical scenarios, such as large 2D
    /// transforms, where custom memory management, buffer reuse, or batched job
    /// scheduling may yield better performance.
    /// </para>
    /// <para>
    /// Nonetheless, the wrappers demonstrate correct usage patterns and can be
    /// safely used as a baseline or starting point for more specialized FFT pipelines.
    /// </para>
    /// </remarks>
    public static class FFTJobDispatcher
    {
        /// <summary>
        /// Schedules a 1D FFT or IFFT job on the given samples.
        /// </summary>
        /// <param name="samples">The complex samples to transform.</param>
        /// <param name="inverse">Whether to perform an inverse FFT.</param>
        /// <param name="normalize">Whether to normalize the result.</param>
        /// <returns>A JobHandle representing the scheduled job.</returns>
        /// <exception cref="ArgumentException">Thrown if the length of samples is not a power of two.</exception>
        private static JobHandle ScheduleFFT_internal(NativeArray<Complex> samples, bool inverse, bool normalize)
        {
            if (!samples.IsCreated || samples.Length == 0)
                throw new ArgumentException("Samples array is not created or has length 0.");

            if (!Mathf.IsPowerOfTwo(samples.Length))
                throw new ArgumentException($"Length of samples ({samples.Length}) must be a power of two for Radix-2 FFT.");

            FFTJob job = new FFTJob
            {
                _Samples = samples,
                _FftInverse = inverse,
                _FftNormalize = normalize
            };
            return UnityJobDispatcher.Schedule(job);
        }

        /// <summary>
        /// Schedules and immediately completes a 1D FFT or IFFT on the given samples.
        /// </summary>
        /// <param name="samples">The complex samples to transform.</param>
        /// <param name="inverse">Whether to perform an inverse FFT.</param>
        /// <param name="normalize">Whether to normalize the result.</param>
        private static void CompleteFFT_internal(NativeArray<Complex> samples, bool inverse, bool normalize)
            => ScheduleFFT_internal(samples, inverse, normalize).Complete();

        /// <summary>
        /// Allocates a ManagedSystemBuffer of Complex numbers with size rounded up to the next power of two.
        /// </summary>
        /// <param name="size">Requested number of elements.</param>
        /// <returns>A persistent ManagedSystemBuffer of Complex numbers.</returns>
        private static ManagedSystemBuffer<Complex> AllocateFFTBuffer(int size)
        {
            SystemBufferDescriptor desc = new SystemBufferDescriptor
            {
                count = Mathf.NextPowerOfTwo(size),
                allocator = Allocator.Persistent
            };
            return new ManagedSystemBuffer<Complex>(desc);
        }

        /// <summary>
        /// Allocates a ManagedSystemBuffer and initializes it with real samples, setting the imaginary parts to zero.
        /// </summary>
        /// <param name="collection">Collection of float samples.</param>
        /// <returns>ManagedSystemBuffer of Complex numbers with imaginary parts set to zero.</returns>
        private static ManagedSystemBuffer<Complex> AllocateAndInitializeFFTBuffer(IEnumerable<float> collection)
        {
            var buffer = AllocateFFTBuffer(collection.Count());

            int i = 0;
            var internalBuffer = buffer.Buffer;

            foreach (var item in collection)
            {
                internalBuffer[i] = new Complex(item, 0);
                i++;
            }

            return buffer;
        }

        /// <summary>
        /// Schedules a forward 1D FFT job.
        /// </summary>
        /// <param name="samples">The complex samples to transform.</param>
        /// <returns>A JobHandle representing the scheduled job.</returns>
        public static JobHandle ScheduleFFT1D(NativeArray<Complex> samples)
            => ScheduleFFT_internal(samples, false, false);

        /// <summary>
        /// Schedules an inverse 1D FFT job.
        /// </summary>
        /// <param name="samples">The complex samples to transform.</param>
        /// <param name="normalize">Whether to normalize the result.</param>
        /// <returns>A JobHandle representing the scheduled job.</returns>
        public static JobHandle ScheduleIFFT1D(NativeArray<Complex> samples, bool normalize = false)
            => ScheduleFFT_internal(samples, true, normalize);

        /// <summary>
        /// Completes a forward 1D FFT immediately.
        /// </summary>
        /// <param name="samples">The complex samples to transform.</param>
        public static void CompleteFFT1D(NativeArray<Complex> samples)
            => CompleteFFT_internal(samples, false, false);

        /// <summary>
        /// Completes an inverse 1D FFT immediately.
        /// </summary>
        /// <param name="samples">The complex samples to transform.</param>
        /// <param name="normalize">Whether to normalize the result.</param>
        public static void CompleteIFFT1D(NativeArray<Complex> samples, bool normalize = false)
            => CompleteFFT_internal(samples, true, normalize);

        /// <summary>
        /// Allocates a buffer from float samples, performs a forward 1D FFT, and returns the buffer.
        /// </summary>
        /// <param name="samples">Collection of float samples to transform.</param>
        /// <returns>ManagedSystemBuffer containing the complex FFT result.</returns>
        public static ManagedSystemBuffer<Complex> CompleteFFT1D(IEnumerable<float> samples)
        {
            var buffer = AllocateAndInitializeFFTBuffer(samples);
            CompleteFFT1D(buffer.Buffer);
            return buffer;
        }

        /// <summary>
        /// Performs a full 2D FFT (or IFFT) on a 1D NativeArray representing
        /// a 2D grid stored in row-major order.
        /// Processing is done column-by-column first, then row-by-row.
        /// The 2D transform is separable, so it can be applied as successive 1D FFTs along each axis.
        /// </summary>
        /// <param name="samples">1D array containing Complex samples in row-major layout.</param>
        /// <param name="width">Width of the 2D data.</param>
        /// <param name="height">Height of the 2D data.</param>
        /// <param name="fftFunc">Delegate to execute a 1D FFT/IFFT.</param>
        private static void CompleteFFT2D_internal(NativeArray<Complex> samples, int width, int height, Action<NativeArray<Complex>> fftFunc)
        {
            if (!samples.IsCreated || samples.Length == 0)
                throw new ArgumentException(nameof(samples));

            if (width * height != samples.Length)
                throw new ArgumentException("width * height must match the length of the samples array.");


            using (var buffer = AllocateFFTBuffer(height))
            {
                for (int x = 0; x < width; ++x)
                {
                    var internalBuffer = buffer.Buffer;

                    for (int y = 0; y < height; ++y)
                    {
                        internalBuffer[y] = samples[y * width + x];
                    }
                    fftFunc.Invoke(internalBuffer);
                    for (int y = 0; y < height; ++y)
                    {
                        samples[y * width + x] = internalBuffer[y];
                    }
                }
            }

            using (var buffer = AllocateFFTBuffer(width))
            {
                for (int y = 0; y < height; ++y)
                {
                    var internalBuffer = buffer.Buffer;

                    for (int x = 0; x < width; ++x)
                    {
                        internalBuffer[x] = samples[y * width + x];
                    }
                    fftFunc.Invoke(internalBuffer);
                    for (int x = 0; x < width; ++x)
                    {
                        samples[y * width + x] = internalBuffer[x];
                    }
                }
            }
        }

        /// <summary>
        /// Performs an in-place 2D forward FFT on a 1D NativeArray representing
        /// a 2D grid in row-major layout.
        /// </summary>
        /// <param name="samples">
        /// Complex sample grid stored as a 1D array in row-major order.
        /// The result overwrites the input.
        /// </param>
        /// <param name="width">Width of the 2D sample grid.</param>
        /// <param name="height">Height of the 2D sample grid.</param>
        public static void CompleteFFT2D(NativeArray<Complex> samples, int width, int height)
            => CompleteFFT2D_internal(samples, width, height, (_) => { CompleteFFT1D(_); });

        /// <summary>
        /// Performs an in-place 2D inverse FFT on a 1D NativeArray representing
        /// a 2D grid in row-major layout.
        /// </summary>
        /// <param name="samples">
        /// Complex frequency-domain samples stored as a 1D array in row-major order.
        /// The result overwrites the input.
        /// </param>
        /// <param name="width">Width of the 2D sample grid.</param>
        /// <param name="height">Height of the 2D sample grid.</param>
        /// <param name="normalize">
        /// If true, divides the result by (width * height), producing a normalized IFFT.
        /// Defaults to false.
        /// </param>
        public static void CompleteIFFT2D(NativeArray<Complex> samples, int width, int height, bool normalize = false)
            => CompleteFFT2D_internal(samples, width, height, (_) => { CompleteIFFT1D(_, normalize); });

        /// <summary>
        /// Schedules a frequency-domain convolution job between two equal-length complex arrays.
        /// </summary>
        /// <param name="samples">The target complex samples in frequency domain, which will be modified in place.</param>
        /// <param name="filter">The complex frequency-domain filter to multiply with.</param>
        /// <returns>A <see cref="JobHandle"/> representing the scheduled convolution job.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if:
        /// <list type="bullet">
        /// <item><description>Either <paramref name="samples"/> or <paramref name="filter"/> is not created.</description></item>
        /// <item><description>Either array has zero length.</description></item>
        /// <item><description>Arrays are not of equal length.</description></item>
        /// </list>
        /// </exception>
        /// <remarks>
        /// This method schedules a pointwise complex multiplication:
        /// <c>samples[k] = samples[k] * filter[k]</c>,
        /// which corresponds to convolution in the time domain.  
        /// This is a convenience wrapper around <see cref="FrequencyConvolutionJob"/>.
        /// </remarks>
        private static JobHandle ScheduleConvolution_internal(NativeArray<Complex> samples, NativeArray<Complex> filter)
        {
            if (!samples.IsCreated || !filter.IsCreated)
                throw new ArgumentException("Samples and filter arrays must be created before convolution.");

            if (samples.Length == 0 || filter.Length == 0)
                throw new ArgumentException("Samples and filter arrays must not have zero length.");

            if (samples.Length != filter.Length)
                throw new ArgumentException(
                    $"Samples and filter must have the same length for frequency-domain convolution. " +
                    $"(samples={samples.Length}, filter={filter.Length})");

            FrequencyConvolutionJob job = new FrequencyConvolutionJob
            {
                _Samples = samples,
                _Filter = filter
            };
            return UnityJobDispatcher.Schedule(job);
        }

        /// <summary>
        /// Schedules and immediately completes a frequency-domain convolution job between two equal-length complex arrays.
        /// </summary>
        /// <param name="samples">The target complex samples in frequency domain, modified in place.</param>
        /// <param name="filter">The complex frequency-domain filter to multiply with.</param>
        /// <remarks>
        /// This is a convenience wrapper for quickly performing a convolution
        /// without manually handling the job scheduling.  
        /// It internally calls <see cref="ScheduleConvolution_internal"/> and completes the job immediately.
        /// </remarks>
        private static void CompleteConvolution_internal(NativeArray<Complex> samples, NativeArray<Complex> filter)
            => ScheduleConvolution_internal(samples, filter).Complete();

        /// <summary>
        /// Public wrapper for scheduling a frequency-domain convolution job.
        /// </summary>
        /// <param name="samples">The target complex samples in frequency domain.</param>
        /// <param name="filter">The complex frequency-domain filter.</param>
        /// <returns>A <see cref="JobHandle"/> for the scheduled convolution job.</returns>
        /// /// <remarks>
        /// This is a convenience wrapper for quickly performing a convolution
        /// without manually handling the job scheduling.  
        /// It internally calls <see cref="CompleteConvolution_internal"/>.
        /// </remarks>
        public static JobHandle ScheduleConvolution(NativeArray<Complex> samples, NativeArray<Complex> filter)
            => ScheduleConvolution_internal(samples, filter);

        /// <summary>
        /// Public wrapper for completing a frequency-domain convolution job immediately.
        /// </summary>
        /// <param name="samples">The target complex samples in frequency domain, modified in place.</param>
        /// <param name="filter">The complex frequency-domain filter.</param>
        /// /// <remarks>
        /// This is a convenience wrapper for quickly performing a convolution
        /// without manually handling the job scheduling.  
        /// It internally calls <see cref="ScheduleConvolution_internal"/> and completes the job immediately.
        /// </remarks>
        public static void CompleteConvolution(NativeArray<Complex> samples, NativeArray<Complex> filter)
            => CompleteConvolution_internal(samples, filter);
    }

    public static class FFTShaderDispatcher
    {
        /// <summary>
        /// Name of the compute shader inside the Resources folder.
        /// Used for performing FFTs over image data.
        /// Loaded through <c>Shader.Find()</c> or <c>Resources.Load()</c>.
        /// </summary>
        private const string k_ComputeFftShaderName = "ComputeFft";

        


    }
}
