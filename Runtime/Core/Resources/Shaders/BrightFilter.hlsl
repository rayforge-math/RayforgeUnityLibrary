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

/// <summary>
/// Hard threshold bright pass: returns the original color if its luminance exceeds the threshold, otherwise black.
/// </summary>
/// <param name="color">Input RGB color.</param>
/// <param name="threshold">Luminance threshold.</param>
/// <returns>Bright-pass filtered color.</returns>
float3 BrightHard(float3 color, float threshold)
{
    float luminance = Luminance(color);
    return luminance < threshold ? (float3) 0 : color;
}

/// <summary>
/// Soft threshold bright pass: subtracts the threshold from each channel and clamps at zero.
/// </summary>
/// <param name="color">Input RGB color.</param>
/// <param name="threshold">Luminance threshold.</param>
/// <returns>Bright-pass filtered color.</returns>
float3 BrightSoft(float3 color, float threshold)
{
    return max(color - threshold, 0);
}

/// <summary>
/// Smooth bright pass: applies a soft transition around the threshold for smoother brightening.
/// </summary>
/// <param name="color">Input RGB color.</param>
/// <param name="threshold">Luminance threshold.</param>
/// <returns>Bright-pass filtered color.</returns>
float3 BrightSmooth(float3 color, float threshold)
{
    float knee = threshold * 0.5;

    float lum = Luminance(color);
    float factor = saturate((lum - threshold + knee) / knee);
    return color * factor;
}

/// <summary>
/// Exponential bright pass: applies a smooth, quadratic curve around the threshold for a stronger highlight falloff.
/// </summary>
/// <param name="color">Input RGB color.</param>
/// <param name="threshold">Luminance threshold.</param>
/// <returns>Bright-pass filtered color.</returns>
float3 BrightExponential(float3 color, float threshold)
{
    float knee = threshold * 0.5;

    float lum = Luminance(color);
    float t = (lum - threshold + knee) / (2.0 * knee);
    t = saturate(t);
    
    t = pow(t, 2.0);

    return color * t;
}

/// <summary>
/// General bright-pass filter selecting one of several modes and optionally clamping the output.
/// </summary>
/// <param name="color">Input RGB color.</param>
/// <param name="threshold">Luminance threshold.</param>
/// <param name="mode">
/// Mode selector:
/// 0 = BrightHard, 1 = BrightSoft, 2 = BrightSmooth, 3 = BrightExponential
/// </param>
/// <param name="clamp">Maximum value to clamp the output color channels.</param>
/// <returns>Bright-pass filtered color.</returns>
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