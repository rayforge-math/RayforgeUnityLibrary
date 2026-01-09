#pragma once

// ============================================================================
// CustomUnityLibrary - Common Shader Include
// Author: Matthew
// Description: pipeline independant bright filter functionality
// ============================================================================

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// ============================================================================
// 1. Constants
// ============================================================================

// ============================================================================
// 2. Utility Functions
// ============================================================================

/// @brief Hard threshold bright pass: returns color if luminance exceeds threshold, else black.
/// @param color Input RGB color
/// @param threshold Luminance threshold
/// @return Bright-pass filtered color
float3 BrightHard(float3 color, float threshold)
{
    float luminance = Luminance(color);
    return luminance < threshold ? (float3) 0 : color;
}

/// @brief Soft threshold bright pass: subtracts threshold and clamps at zero.
/// @param color Input RGB color
/// @param threshold Luminance threshold
/// @return Bright-pass filtered color
float3 BrightSoft(float3 color, float threshold)
{
    return max(color - threshold, 0);
}

/// @brief Smooth bright pass: soft transition around threshold for smooth brightening.
/// @param color Input RGB color
/// @param threshold Luminance threshold
/// @return Bright-pass filtered color
float3 BrightSmooth(float3 color, float threshold)
{
    float knee = threshold * 0.5;
    float lum = Luminance(color);
    float factor = saturate((lum - threshold + knee) / knee);
    return color * factor;
}

/// @brief Exponential bright pass: quadratic curve around threshold for stronger highlight falloff.
/// @param color Input RGB color
/// @param threshold Luminance threshold
/// @return Bright-pass filtered color
float3 BrightExponential(float3 color, float threshold)
{
    float knee = threshold * 0.5;
    float lum = Luminance(color);
    float t = (lum - threshold + knee) / (2.0 * knee);
    t = saturate(t);
    t = pow(t, 2.0);
    return color * t;
}

/// @brief General bright-pass filter selecting mode and optional clamping.
/// @param color Input RGB color
/// @param threshold Luminance threshold
/// @param mode Filter mode: 0=Hard, 1=Soft, 2=Smooth, 3=Exponential
/// @param clamp Maximum value to clamp output color channels
/// @return Bright-pass filtered color
float3 BrightFilter(float3 color, float threshold, int mode, float clamp)
{
    float3 result = (float3) 0;
    switch (mode)
    {
        default:
        case 0:
            result = BrightHard(color, threshold);
            break;
        case 1:
            result = BrightSoft(color, threshold);
            break;
        case 2:
            result = BrightSmooth(color, threshold);
            break;
        case 3:
            result = BrightExponential(color, threshold);
            break;
    }
    return min(result, clamp);
}