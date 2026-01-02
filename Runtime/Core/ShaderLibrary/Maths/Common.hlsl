#pragma once

// ============================================================================
// 1. Utility Functions
// ============================================================================

/// @brief Fast cosine approximation using a 5th-order Taylor series.
/// Accurate for inputs in the range [0, PI].
/// @param x Input angle in radians.
/// @return Approximate cosine of x.
inline float CosApprox(float x)
{
    float s = PI / 2.0 - x;
    float s2 = s * s;
    float s3 = s2 * s;
    float s5 = s3 * s2;
    return s - s3 / 6.0 + s5 / 120.0;
}

/// @brief Fast sine approximation using a 6th-order Taylor series.
/// Accurate for inputs in the range [0, PI].
/// @param x Input angle in radians.
/// @return Approximate sine of x.
inline float SinApprox(float x)
{
    float s = x - PI / 2.0;
    float s2 = s * s;
    float s4 = s2 * s2;
    return 1.0 - s2 / 2.0 + s4 / 24.0;
}

