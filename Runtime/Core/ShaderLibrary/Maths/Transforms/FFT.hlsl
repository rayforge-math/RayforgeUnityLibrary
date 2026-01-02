#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#include "Packages/eu.rayforge.unitylibrary/Runtime/Core/ShaderLibrary/Maths/Spaces/ComplexPlane.hlsl"

// ============================================================================
// 1. Inputs
// ============================================================================

/// @brief FFT configuration parameters.
///
/// @details
/// The first three fields (`_FftLength`, `_FftInverse`, `_FftNormalize`)
/// are used directly by the FFT functions.
/// `_FftParallelRowCount` is not part of the FFT math — it's intended for
/// external use when dispatching multiple FFT rows in parallel.
CBUFFER_START(_FftParams)
int _FftLength;
bool _FftInverse;
bool _FftNormalize;
int _FftParallelRowCount;
CBUFFER_END

/// @brief Returns the FFT length configured for the current transform.
/// @return Integer value representing the number of samples processed by the FFT.
int GetLength()
{
    return _FftLength;
}
// ============================================================================
// 2. Prototypes - for abstracting the precise data layout
// ============================================================================

/// @brief Retrieves a complex sample at the given index.
/// @param baseOffset Offset of segment within data structure.
/// @param index Zero-based array index.
/// @return The complex sample stored at the specified index.
Complex GetSample(int baseOffset, int index);

/// @brief Writes a complex sample into the underlying buffer.
/// @param baseOffset Offset of segment within data structure.
/// @param index Zero-based array index.
/// @param sample Complex value to store.
void SetSample(int baseOffset, int index, Complex sample);

/// @brief Retrieves a complex filter coefficient at the given index.
/// @param index Zero-based array index.
/// @return Complex filter coefficient.
Complex GetFilter(int index);

// ============================================================================
// 3. Utility Functions
// ============================================================================

// --- HLSL-Bridge ---
// (Functions in this region make hlsl-style code compatible with C# functionality)

/// @brief Computes the twiddle factor e^(i * angle), a unit-magnitude complex number.
/// @param angle Angle in radians.
/// @return Complex number representing e^(i·angle).
Complex TwiddleFactor(float angle)
{
    Polar p = (Polar) 0;
    p.value = float2(1.0f, angle);
    return PolarToComplex(p);
}

// end --- HLSL-Bridge ---

// --- C#-Compatible FFT ---
// This section mirrors the C# FFT implementation exactly.
// Functions here are written in HLSL but reflect the C# logic 1:1.
// You can copy the logic to C# with minimal changes.

/// @brief Computes the integer base-2 logarithm of N (assumes N = 2^n)
/// @param N Length of the array (power of 2)
/// @return Number of bits required to represent indices
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

/// @brief Computes the bit-reversed index for a given index x
/// @param x Original index
/// @param log2n Number of bits
/// @return Bit-reversed index
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

/// @brief Reorders the elements in the buffer segment using bit-reversal ordering.
/// @details Bit-reversal is a prerequisite for Cooley-Tukey radix-2 FFT. 
/// Separating this step allows modularity: different stages of FFT can reuse this logic.
/// @param baseOffset Offset into the buffer for the segment.
/// @param N Number of elements in the segment.
/// @param bits Number of bits used for indexing (log2(N)).
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

/// @brief Performs a single DFT on a sub-segment (butterfly computation).
/// @details Modularizing the inner loop allows parallel execution per thread
/// and makes it easier to replace or optimize the core computation.
/// @param baseOffset Offset into the buffer for the segment.
/// @param k Start index of the current block within the segment.
/// @param m2 Half-length of the current sub-FFT (number of butterfly pairs).
/// @param theta Twiddle angle factor for the current stage.
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

/// @brief Performs one FFT stage (all butterfly computations for sub-FFT length m).
/// @details Modular stage function separates stage iteration from inner DFT logic.
/// This is helpful for GPU mapping and flexible pipeline assembly.
/// @param baseOffset Offset into the buffer for the segment.
/// @param N Total length of the FFT segment.
/// @param m Length of the current sub-FFT (m).
/// @param theta Twiddle angle factor for this stage.
void FFTStage(int baseOffset, int N, int m, float theta)
{
    int m2 = m >> 1;                    // half-length of the block

    for (int k = 0; k < N; k += m)      // iterate over each block
    {
        DFT(baseOffset, k, m2, theta);
    }
}

/// @brief Executes all FFT stages (radix-2) for a buffer segment.
/// @details Modularizing stage iteration allows combining stages differently
/// (e.g., splitting into GPU dispatches or parallel rows) and makes it easier
/// to adapt for 2D FFTs or multi-segment processing.
/// @param baseOffset Offset into the buffer for the segment.
/// @param N Number of elements in the segment.
/// @param bits Number of bits used for indexing (log2(N)).
void Radix2FFT(int baseOffset, int N, int bits)
{
    for (int s = 1; s <= bits; ++s)
    {
        int m = 1 << s;
        float theta = (_FftInverse ? 1.0f : -1.0f) * 2.0f * PI / m;

        FFTStage(baseOffset, N, m, theta);
    }
}

/// @brief Normalizes the buffer segment if performing an inverse FFT.
/// @details Separate normalization keeps the main FFT loop clean and modular.
/// @param baseOffset Offset into the buffer for the segment.
/// @param N Number of elements in the segment.
void NormalizeIfInverse(int baseOffset, int N)
{
    if (!_FftInverse || !_FftNormalize)
        return;

    float invN = 1.0f / N;
    for (int i = 0; i < N; ++i)
    {
        SetSample(baseOffset, i, ComplexScale(GetSample(baseOffset, i), invN));
    }
}

/// @brief Performs an in-place iterative Cooley-Tukey radix-2 FFT or IFFT.
/// @details Demonstrates modular FFT design:
/// - BitReversal prepares the data,
/// - Radix2FFT iterates over stages,
/// - NormalizeIfInverse applies optional normalization.
/// This modular approach allows flexible use for 1D or 2D FFTs,
/// multi-segment buffers, GPU parallelization with sync, or testing individual modules.
/// @param baseOffset Offset into the buffer for the segment to transform.
void FFT(int baseOffset)
{
    int N = GetLength();
    int bits = lg2(N);

    BitReversal(baseOffset, N, bits);
    Radix2FFT(baseOffset, N, bits);
    NormalizeIfInverse(baseOffset, N);
}

// end --- C#-Compatible FFT ---

// --- C#-Compatible FFT Normalize ---
// This section mirrors the C# FFT normalization exactly.
// Functions here are written in HLSL but reflect the C# logic 1:1.
// You can copy the logic to C# with minimal changes.

/// @brief Normalizes an array of complex numbers by dividing each element by the FFT length.
void NormalizeFFT(int baseOffset)
{
    float invN = 1.0f / GetLength();

    for (int i = 0; i < GetLength(); ++i)
    {
        SetSample(baseOffset, i, ComplexScale(GetSample(baseOffset, i), invN));
    }
}

// end --- C#-Compatible FFT Normalize ---

// --- C#-Compatible Frequency Domain Convolution ---
// (Functions in this region mirror their HLSL equivalents)

/// @brief: Performs pointwise complex multiplication (Convolution in frequency domain): S[k] = S[k] * F[k]
void Convolute(int baseOffset)
{
    for (int i = 0; i < GetLength(); ++i)
    {
        SetSample(baseOffset, i, ComplexMul(GetSample(baseOffset, i), GetFilter(i)));
    }
}

// end --- C#-Compatible Frequency Domain Convolution ---
