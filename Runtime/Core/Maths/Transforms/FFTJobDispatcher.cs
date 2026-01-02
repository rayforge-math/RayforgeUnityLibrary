using Rayforge.ManagedResources.NativeMemory;
using Rayforge.Maths.Tranforms;
using Rayforge.Utility.Threading;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

using Rayforge.Maths.Spaces;
using System.Linq;

namespace Rayforge.Maths.Transforms
{
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
        /// <exception cref="ArgumentException">Thrown if the length of samples is not a power of two or the input is not valid.</exception>
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
}