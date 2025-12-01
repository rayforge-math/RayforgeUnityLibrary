#pragma once

#include "Packages/eu.rayforge.unitylibrary/Runtime/Core/ShaderLibrary/Maths/Coords.hlsl"

RWStructuredBuffer<Complex> _FftSamples;
StructuredBuffer<Complex> _FftFilter;

CBUFFER_START(_FftParams)
int _FftLength;
bool _FftInverse;
bool _FftNormalize;
CBUFFER_END

// ============================================================================
// 2. Utility Functions
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
void FFT()
{
    int N = _FftLength;
    int bits = lg2(N);

    // Performs in-place bit reversal on the array to prepare for FFT, 
    // e.g. 0, 1, 2, 3, 4, 5, 6, 7 -> 0, 4, 1, 5, 2, 6, 3, 7
    for (int i = 0; i < N; ++i)
    {
        int j = BitReverse(i, bits);
        if (i < j)
        {
            Complex tmp = _FftSamples[i];
            _FftSamples[i] = _FftSamples[j];
            _FftSamples[j] = tmp;
        }
    }

    // Performs actual FFT
    for (int s = 1; s <= bits; ++s)
    {
        int m = 1 << s; // current sub-FFT length
        int m2 = m >> 1; // half-length
        float theta = (_FftInverse ? 1.0f : -1.0f) * 2.0f * PI / m; // twiddle angle

        for (int k = 0; k < N; k += m)                              // iterate over blocks
        {
            for (int j = 0; j < m2; ++j)                            // iterate over block elements
            {
                float ang = theta * j;
                Complex w = TwiddleFactor(ang);

                Complex p = _FftSamples[k + j];
                Complex q = ComplexMul(w, _FftSamples[k + j + m2]);

                _FftSamples[k + j] = ComplexAdd(p, q);
                _FftSamples[k + j + m2] = ComplexSub(p, q);
            }
        }
    }

    if (_FftInverse && _FftNormalize)
    {
        float invN = 1.0f / _FftLength;
        for (int i = 0; i < _FftLength; ++i)
        {
            _FftSamples[i] = ComplexScale(_FftSamples[i], invN);
        }
    }
}

// end --- C#-Compatible FFT ---

// --- C#-Compatible FFT Normalize ---
// This section mirrors the C# FFT normalization exactly.
// Functions here are written in HLSL but reflect the C# logic 1:1.
// You can copy the logic to C# with minimal changes.

/// @brief Normalizes an array of complex numbers by dividing each element by the FFT length.
void NormalizeFFT()
{
    float invN = 1.0f / _FftLength;

    for (int i = 0; i < _FftLength; ++i)
    {
        _FftSamples[i] = ComplexScale(_FftSamples[i], invN);
    }
}

// end --- C#-Compatible FFT Normalize ---

// --- C#-Compatible Frequency Domain Convolution ---
// (Functions in this region mirror their HLSL equivalents)

/// @brief: Performs pointwise complex multiplication (Convolution in frequency domain): S[k] = S[k] * F[k]
void Convolute()
{
    for (int i = 0; i < _FftLength; ++i)
    {
        _FftSamples[i] = ComplexMul(_FftSamples[i], _FftFilter[i]);
    }
}

// end --- C#-Compatible Frequency Domain Convolution ---
