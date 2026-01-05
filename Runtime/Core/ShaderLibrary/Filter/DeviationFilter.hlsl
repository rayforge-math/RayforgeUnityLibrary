#pragma once

// ============================================================================
// CustomUnityLibrary - Common Shader Include
// Author: Matthew
// Description: pipeline independant bright filter functionality
// ============================================================================

// ============================================================================
// 1. Includes
// ============================================================================

#include "Packages/eu.rayforge.unitylibrary/Runtime/Core/ShaderLibrary/Maths/Statistics.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// ============================================================================
// 1. Defins
// ============================================================================

// Clamp / Dampen modes
#define CLAMP_NONE       0
#define CLAMP_MINMAX     1
#define CLAMP_VARIANCE   2
#define CLAMP_CLIPBOX    3

// ============================================================================
// 3. Utility Functions
// ============================================================================

/// @brief Performs variance-based clip-box clamping on an input color using the mean
/// and standard deviation of a 3x3 neighborhood from the current frame.
/// Can be used for temporal history clamping or purely spatial outlier suppression.
/// @param inputColor The color to be clamped (history or current-frame).
/// @param neighborhood A fixed array of 9 float3 samples representing the local 3x3 neighborhood.
/// @param scale Controls the width of the variance clip box. Typical values: 1.0�3.0.
/// @return The input color clamped to [mean - stdDev * scale, mean + stdDev * scale].
float3 VarianceClamp(float3 inputColor, float4 neighborhood[9], float scale)
{
    float3 mean, stdDev;
    ComputeMeanAndStdDev9(neighborhood, mean, stdDev);

    float3 minC = mean - stdDev * scale;
    float3 maxC = mean + stdDev * scale;

    return clamp(inputColor, minC, maxC);
}

/// @brief Performs luma-oriented clip-box clamping on an input color using the
/// 3x3 neighborhood from the current frame. The clip box is aligned along the
/// principal luminance direction.
/// This approach is similar to Unreal Engine's TAA clip-box clamping, but is
/// fully usable for spatial-only filtering as well.
/// This is roughly the approach Unreal Engine uses for temporal AA: "https://de45xmedrsdbp.cloudfront.net/Resources/files/TemporalAA_small-59732822.pdf#page=34"
/// @param inputColor The color to be clamped (history or current-frame).
/// @param neighborhood A fixed array of 9 float3 samples representing the local 3x3 neighborhood.
/// @param scale Controls the width of the clip box. Typical values: 1.0�3.0.
/// @return The clamped color.
float3 ClipBoxClamp(float3 inputColor, float4 neighborhood[9], float scale)
{
    float3 mean, stdDev;
    ComputeMeanAndStdDev9(neighborhood, mean, stdDev);

    float3 lumaWeights = float3(0.2126, 0.7152, 0.0722);
    float meanLuma = dot(mean, lumaWeights);

    float3 lumaDir = float3(0, 0, 0);
    [unroll]
    for (int i = 0; i < 9; ++i)
    {
        float3 delta = neighborhood[i].rgb - mean;
        float lumaDelta = dot(neighborhood[i].rgb, lumaWeights) - meanLuma;
        lumaDir += delta * lumaDelta;
    }
    lumaDir = normalize(lumaDir + 1e-6);

    float3 deltaInput = inputColor - mean;
    float proj = dot(deltaInput, lumaDir);

    float limit = length(stdDev) * scale;
    float3 clampedDelta = clamp(proj, -limit, limit) * lumaDir;

    return mean + clampedDelta;
}

/// @brief Clamps an input color to the min/max bounding box defined by a 3x3
/// neighborhood from the current frame.
/// Useful as a robust safety clamp for both temporal and spatial filtering.
/// @param inputColor The color to be clamped (history or current-frame).
/// @param neighborhood A fixed array of 9 float3 samples representing the local 3x3 neighborhood.
/// @param scale Scales the range around the average value of the min and max color value.
/// @return The clamped color.
float3 MinMaxClamp(float3 inputColor, float4 neighborhood[9], float scale)
{
    float3 minColor = float3(1e9, 1e9, 1e9);
    float3 maxColor = float3(-1e9, -1e9, -1e9);

    [unroll]
    for (int i = 0; i < 9; ++i)
    {
        minColor = min(minColor, neighborhood[i].rgb);
        maxColor = max(maxColor, neighborhood[i].rgb);
    }

    float3 centre = (maxColor + minColor) * 0.5;
    float3 halfExtent = (maxColor - minColor) * 0.5 * scale;

    float3 scaledMin = centre - halfExtent;
    float3 scaledMax = centre + halfExtent;

    return clamp(inputColor, scaledMin, scaledMax);
}

/// @brief Applies a selected color clamping or deviation-damping mode to an input color
///        based on the local 3x3 neighborhood.
/// @details Can be used for temporal history clamping or purely spatial outlier suppression.
///          The mode is selected via an integer or #define constant:
///          - 0 = None (no clamping)
///          - 1 = Min/Max Clamp (scaled around min/max midpoint)
///          - 2 = Variance Clamp (mean � stdDev * scale)
///          - 3 = ClipBox Clamp (luma-oriented, UE-style)
/// @param inputColor The input color to clamp/damp (history or current-frame).
/// @param neighborhood A fixed array of 9 float3 samples representing the local 3x3 neighborhood.
/// @param mode Clamping / damping mode to apply (see above).
/// @param scaleOrStrength Scale factor for clamps (MinMax, Variance, ClipBox) or strength for damping (0�1).
/// @return The resulting color after applying the selected clamping or damping mode.
float3 ApplyColorClamping(float3 inputColor, float4 neighborhood[9], int mode, float scale)
{
    switch (mode)
    {
        default:
        case CLAMP_NONE:
            break;

        case CLAMP_MINMAX:
            inputColor = MinMaxClamp(inputColor, neighborhood, scale);
            break;

        case CLAMP_VARIANCE:
            inputColor = VarianceClamp(inputColor, neighborhood, scale);
            break;

        case CLAMP_CLIPBOX:
            inputColor = ClipBoxClamp(inputColor, neighborhood, scale);
            break;
    }

    return inputColor;
}

/// @brief Smoothly damps deviations from the local mean based on standard deviation.
/// Works for both temporal (history) and purely spatial (current-frame) inputs.
/// Large deviations are pulled closer to the mean without hard clamping.
/// @param inputColor The color to be damped (history or current-frame).
/// @param neighborhood A fixed array of 9 float3 samples representing the local 3x3 neighborhood.
/// @param strength How strongly to pull the color towards the local mean (0 = off, 1 = full damping).
/// @param threshold Deviation at which damping starts to take effect (in units of sigma).
/// @return The damped color.
float3 StdDevSmoothen(float3 inputColor, float4 neighborhood[9], float strength, float threshold)
{
    float3 mean, stdDev;
    ComputeMeanAndStdDev9(neighborhood, mean, stdDev);

    float delta = Luminance(inputColor) - Luminance(mean);
    float deviation = delta / (Luminance(stdDev) + 1e-6);

    float t = (deviation - threshold) / max(threshold, 1e-6);

    return lerp(inputColor, mean, saturate(t * strength));
}

/// @brief Smoothly dampens bright outlier pixels in a 3x3 neighborhood based on local luminance variance.
/// @details The damping strength increases with local luminance variation and optionally with how far
///          the central pixel deviates from the local mean. This preserves coherent highlights while
///          suppressing small, high-intensity spikes (fireflies).
/// @param neighborhood A fixed array of 9 float4 samples representing the local 3x3 neighborhood.
///                     Only the RGB channels are used; alpha is left unchanged.
/// @param strength Controls the overall damping intensity (0 = no damping).
/// @param proportional If true, damping is scaled by how strongly the center pixel deviates from the
///                     local mean. If false, damping depends only on neighborhood variance.
/// @return The damped color of the central pixel (neighborhood[4].rgb).
float3 StdDevDampen(float4 neighborhood[9], float strength, bool proportional)
{
    float3 mean, stdDev;
    ComputeMeanAndStdDev9(neighborhood, mean, stdDev);

    float3 centre = neighborhood[4].rgb;
    
    float meanLuma = Luminance(mean);
    float centreLuma = Luminance(centre);
    float stdDevLuma = Luminance(stdDev);

    float deltaLuma = max(centreLuma - meanLuma, 0.0);

    float dampen;
    if (proportional)
    {
        dampen = saturate(strength * (deltaLuma * stdDevLuma));
    }
    else
    {
        dampen = saturate(strength * (step(0.0, deltaLuma) * stdDevLuma));
    }

    return centre * (1.0 - dampen);
}