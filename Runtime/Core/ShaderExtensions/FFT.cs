using Rayforge.ManagedResources.NativeMemory;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayforge.ShaderExtensions.FFT
{
    public static class FFTParameters
    {
        /// <summary>
        /// Shader property ID for <c>_FftSamples</c>.
        /// Used to supply complex samples to FFT compute shaders.
        /// </summary>
        public static int FftSamplesId => k_FftSamplesId;
        private static readonly int k_FftSamplesId = Shader.PropertyToID("_FftSamples");

        /// <summary>
        /// Shader property ID for <c>_FftFilter</c>.
        /// Represents a frequency-domain filter used during convolution.
        /// </summary>
        public static int FftFilterId => k_FftFilterId;
        private static readonly int k_FftFilterId = Shader.PropertyToID("_FftFilter");

        /// <summary>
        /// Shader property ID for <c>_FftLength</c>.
        /// Defines the FFT size (number of samples in 1D).
        /// </summary>
        /// <returns>Integer property ID.</returns>
        public static int FftLength => k_FftLengthId;
        private static readonly int k_FftLengthId = Shader.PropertyToID("_FftLength");

        /// <summary>
        /// Shader property ID for <c>_FftInverse</c>.
        /// 0 = forward FFT, 1 = inverse FFT.
        /// </summary>
        public static int FftInverse => k_FftInverseId;
        private static readonly int k_FftInverseId = Shader.PropertyToID("_FftInverse");

        /// <summary>
        /// Shader property ID for <c>_FftNormalize</c>.
        /// Indicates whether an FFT output should be normalized.
        /// </summary>
        public static int FftNormalize => k_FftNormalizeId;
        private static readonly int k_FftNormalizeId = Shader.PropertyToID("_FftNormalize");

        /// <summary>
        /// Structure containing packed FFT parameters that are sent to a compute shader
        /// via a constant buffer.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        struct FftParams : IComputeData<FftParams>
        {
            /// <summary>
            /// FFT length (number of samples).
            /// </summary>
            public int _FftLength;

            /// <summary>
            /// FFT direction flag.  
            /// 0 = forward FFT, 1 = inverse FFT.
            /// </summary>
            public int _FftInverse;

            /// <summary>
            /// Whether to apply normalization after inverse FFT.
            /// </summary>
            public int _FftNormalize;

            /// <summary>
            /// Number of FFT rows processed in parallel for each compute dispatch.
            /// Controls how many rows are batched together on the GPU.
            /// 
            /// For 1D FFTs, this value has no effect. It is only used when performing
            /// multiple 2D FFTs in parallel, allowing them to share the same constant buffer.
            /// </summary>
            public int _FftParallelRowCount;

            /// <summary>
            /// Returns this struct as raw compute-shader-compatible data.
            /// </summary>
            public FftParams RawData => this;
        }

        /// <summary>
        /// Shader property ID for the constant buffer <c>_FftParams</c> using <see cref="FftParams"/>
        /// Provides bundled FFT parameters to compute shaders.
        /// </summary>
        public static int FftParamsId => k_FftParamsId;
        private static readonly int k_FftParamsId = Shader.PropertyToID("_FftParams");
    }
}