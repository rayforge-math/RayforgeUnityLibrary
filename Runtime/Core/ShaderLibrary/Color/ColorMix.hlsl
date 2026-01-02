#pragma once

// ============================================================================
// 1. Includes
// ============================================================================

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// ============================================================================
// 2. Utility Functions
// ============================================================================

/// @brief Blends a base color with a secondary color according to the specified mixing mode and strength.
/// @param color The original color to be modified.
/// @param mixColor The color used for blending or scaling.
/// @param mixOption The mixing mode: 
/// 0 = No change, returns color unchanged.
/// 1 = Multiply blend: mixColor * color interpolated by strength.
/// 2 = Scale by luminance: scales mixColor by the ratio of luminances.
/// 3 = Additive blend: mixColor * luminance + color interpolated by strength.
/// @param strength Interpolation factor for the blending, typically in range 0..1.
/// @param clamp If true, clamps the base color luminance to 0..1 before computations.
/// @return The resulting blended color as float3.
float3 MixColor(float3 color, float3 mixColor, int mixOption, float strength, bool clamp)
{
    float luminance = Luminance(color);
    if (clamp)
    {
        luminance = saturate(luminance);
    }

    switch (mixOption)
    {
        default:
        case 0:
            return color;
        case 1:
            return lerp(color, mixColor * color, strength);
        case 2:
            float lutLuminance = saturate(Luminance(mixColor));
            float scale = luminance / max(lutLuminance, 1e-5);
            return lerp(color, mixColor * scale, strength);
        case 3:
            return lerp(color, mixColor * luminance + color, strength);
    }
}

/// @brief Applies a 1D LUT (lookup texture) to the input color and blends it using the specified mixing mode and strength.
/// @param lut The lookup texture containing the color grading curve.
/// @param color The original color to modify.
/// @param mode The mixing mode passed to MixColor: 0 = No change, 1 = Multiply blend, 2 = Scale by luminance, 3 = Additive blend.
/// @param strength The interpolation factor for blending, typically in range 0..1.
/// @return The resulting color after LUT application and blending as float3.
float3 MixLut(TEXTURE2D(lut), float3 color, int mode, float strength)
{
    float luminance = saturate(Luminance(color));
    float3 lutColor = SAMPLE_TEXTURE2D(lut, sampler_LinearClampCustom, float2(luminance, 0.5)).rgb;

    color = MixColor(color, lutColor, mode, strength, true);

    return color;
}
