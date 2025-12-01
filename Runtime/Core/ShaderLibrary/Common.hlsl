#pragma once

// ============================================================================
// CustomUnityLibrary - Common Shader Include
// Author: Matthew
// Description: pipeline independant HLSL utilities for Unity
// ============================================================================

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// ============================================================================
// 1. Constants
// ============================================================================

static const float4 Base_TexelSize = float4(1.0 / 1920, 1.0 / 1080, 1920, 1080);

SamplerState sampler_LinearClampCustom
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};

// ============================================================================
// 2. Utility Functions
// ============================================================================

// ============================================================================
// Sketch:
// (0,0)                     (1,0)                    (2,0)         <- uvs
// (-1,-1)                   (1,-1)                   (3,-1)        <- vert (CS)
// _____________________________________________________
// |                           |                    /
// |                           |                /
// |             VS            |            /
// |                           |        /
// |                           |    /
// |___________________________ /
// |                        /
// |                    /
// |                /
// |            /
// |        /
// |    /
// |/
// ============================================================================
/// @brief FullscreenTriangle Vertex Helper.
/// Procedurally generates the vertices and UVs for a single fullscreen triangle
/// to cover the entire render target. Works efficiently with
/// DrawProcedural or full-screen passes without a vertex buffer.
/// @param id Vertex index (0, 1, 2)
/// @param positionCS Output: Clip-space position (SV_POSITION) in range -1..3
/// @param texcoord Output: UV coordinates in the range 0..2
void FullscreenTriangle(uint id, out float4 positionCS, out float2 texcoord)
{
    texcoord = float2((id << 1) & 2, id & 2);
    positionCS = float4(texcoord * 2 - 1, 0, 1);
}

/// @brief Computes a mirrored UV coordinate around the texture center along a given direction.
/// @param texcoord Input UV coordinate.
/// @param dir Normalized direction to mirror along.
/// @return Mirrored UV coordinate.
float2 MirrorUv(float2 texcoord, float2 dir)
{
    static const float2 centre = float2(0.5, 0.5);

    float2 p = texcoord - centre;
    float2 proj = dot(p, dir) * dir;
    float2 mirrored = 2 * proj - p;

    return mirrored + centre;
}

/// @brief Computes a pseudo-radial value in the range 0..1 based on a normalized texture coordinate.
/// Can be used for radial gradients, scattering, or angular effects around the center (0.5, 0.5).
/// @param texcoord Normalized texture coordinate (0..1) for which to compute the radial value.
/// @param scatter Scaling factor for the pseudo-angle. Larger values increase angular spread.
/// @return A pseudo-radial value in the range 0..1.
float Radial01(float2 texcoord, float scatter)
{
    float2 dir = texcoord - 0.5;
    float pseudoAngle = dir.y / (abs(dir.x) + abs(dir.y)) * scatter;
    pseudoAngle = abs(pseudoAngle);
    return pseudoAngle;
}

/// @brief Computes a curvature-based UV offset for a given texture coordinate.
/// Useful for barrel or pincushion distortion effects.
/// @param texcoord Input UV coordinates (0..1).
/// @param strength Distortion intensity factor.
/// @param screenSize The size of the target screen.
/// @return A float2 offset to add to the original UV for the curvature effect.
float2 CurvatureOffset(float2 texcoord, float strength, float2 screenSize)
{
    float2 centered = texcoord * 2.0 - 1.0;
    centered.y *= (screenSize.y / screenSize.x);

    float r2 = dot(centered, centered);
    float2 curvature = centered * (1.0 - r2) * strength * 0.5;
    return curvature;
}

/// @brief Computes a warp-style UV offset, emphasizing near-center distortion and radial pull.
/// Can be used for fish-eye or lens-style warp effects.
/// @param texcoord Input UV coordinates (0..1).
/// @param strength Overall intensity of the warp.
/// @param shape Aspect ratio influence (0 = no aspect correction, 1 = correct for screen aspect).
/// @param screenSize The size of the target screen.
/// @return A float2 offset to apply to the original UV coordinates for the warp effect.
float2 WarpOffset(float2 texcoord, float strength, float shape, float2 screenSize)
{
    float2 centered = texcoord - 0.5;
    centered.y *= lerp(1.0, (screenSize.y / screenSize.x), shape);

    float r2 = dot(centered, centered);
    float2 offset = centered / max(r2, 1e-5) * strength * -0.25;

    offset.y *= lerp(1.0, (screenSize.x / screenSize.y), shape);
    return offset;
}

/// @brief Computes a complementary color by subtracting the input color from the maximum component value.
/// The result is a simple complementary approximation that preserves intensity per channel.
/// @param color Input RGB color vector (0..1 range recommended).
/// @return Complementary RGB color vector.
float3 Complementary(float3 color)
{
    float3 n = max(color.r, max(color.g, color.b)).rrr;
    return n - color;
}

/// @brief Aligns a UV coordinate to the nearest texel boundary using the given texel size.
/// Useful for eliminating subpixel jitter or ensuring pixel-perfect sampling.
/// @param texcoord The UV coordinate to align.
/// @param texelSize The size of a single texel in UV space (typically float2(1/width, 1/height)).
/// @return The UV coordinate snapped to the nearest texel grid.
float2 AlignToTexel(float2 texcoord, float2 texelSize)
{
    return texelSize * round(texcoord / texelSize);
}

/// @brief Checks whether the given UV coordinates are within the normalized [0,1] range.
/// @param uv The UV coordinates to test.
/// @param cutoff If true, enforces the UV bounds check; if false, the function always returns true.
/// @return True if the UV coordinates are within bounds or cutoff is false; false otherwise.
bool UvInBounds(float2 uv, bool cutoff)
{
    return !cutoff || (0.0 <= uv.x && uv.x <= 1.0 && 0.0 < uv.y && uv.y <= 1.0);
}

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
