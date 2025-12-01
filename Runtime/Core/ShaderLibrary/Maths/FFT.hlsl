#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/eu.rayforge.unitylibrary/Runtime/Core/ShaderLibrary/Maths/Coords.hlsl"

// ============================================================================
// 1. Inputs
// ============================================================================

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
/// @param index Zero-based array index.
/// @return The complex sample stored at the specified index.
Complex GetSample(int baseOffset, int index);

/// @brief Writes a complex sample into the underlying buffer.
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

/// @brief Performs an in-place iterative FFT or inverse FFT
/// @details Cooley-Tukey radix-2 algorithm, bit-reversed ordering
/// @note Mirrors the C# FFT implementation exactly
/// @param baseOffset: baseOffset of the unterlying array.
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
