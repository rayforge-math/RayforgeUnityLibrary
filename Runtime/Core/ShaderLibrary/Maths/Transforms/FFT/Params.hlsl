#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

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