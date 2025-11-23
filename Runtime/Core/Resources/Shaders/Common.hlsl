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
/// <summary>
/// FullscreenTriangle Vertex Helper.
/// Procedurally generates the vertices and UVs for a single fullscreen triangle
/// to cover the entire render target. Works efficiently with
/// DrawProcedural or full-screen passes without a vertex buffer.
/// </summary>
/// <param name="id">Vertex index (0, 1, 2)</param>
/// <param name="positionCS">Output: Clip-space position (SV_POSITION) in range -1..3</param>
/// <param name="texcoord">Output: UV coordinates in the range 0..2</param>
void FullscreenTriangle(uint id, out float4 positionCS, out float2 texcoord)
{
    texcoord = float2((id << 1) & 2, id & 2);
    positionCS = float4(texcoord * 2 - 1, 0, 1);
}

/// <summary>
/// Computes a mirrored UV coordinate around the texture center along a given direction.
/// </summary>
/// <param name="texcoord">Input UV coordinate.</param>
/// <param name="dir">Normalized direction to mirror along.</param>
/// <returns>Mirrored UV coordinate.</returns>
float2 MirrorUv(float2 texcoord, float2 dir)
{
    static const float2 centre = float2(0.5, 0.5);

    float2 p = texcoord - centre;
    float2 proj = dot(p, dir) * dir;
    float2 mirrored = 2 * proj - p;

    return mirrored + centre;
}

/// <summary>
/// Computes a pseudo-radial value in the range 0..1 based on a normalized texture coordinate.
/// Can be used for radial gradients, scattering, or angular effects around the center (0.5, 0.5).
/// </summary>
/// <param name="texcoord">Normalized texture coordinate (0..1) for which to compute the radial value.</param>
/// <param name="scatter">Scaling factor for the pseudo-angle. Larger values increase angular spread.</param>
/// <returns>A pseudo-radial value in the range 0..1.</returns>
float Radial01(float2 texcoord, float scatter)
{
    float2 dir = texcoord - 0.5;
    float pseudoAngle = dir.y / (abs(dir.x) + abs(dir.y)) * scatter;
    pseudoAngle = abs(pseudoAngle);
    return pseudoAngle;
}

/// <summary>
/// Computes a curvature-based UV offset for a given texture coordinate.
/// Useful for barrel or pincushion distortion effects.
/// </summary>
/// <param name="texcoord">Input UV coordinates (0..1).</param>
/// <param name="strength">Distortion intensity factor.</param>
/// <param name="screenSize">The size of the target screen.</param>
/// <returns>A float2 offset to add to the original UV for the curvature effect.</returns>
float2 CurvatureOffset(float2 texcoord, float strength, float2 screenSize)
{
    float2 centered = texcoord * 2.0 - 1.0;
    centered.y *= (screenSize.y / screenSize.x);

    float r2 = dot(centered, centered);
    float2 curvature = centered * (1.0 - r2) * strength * 0.5;
    return curvature;
}

/// <summary>
/// Computes a warp-style UV offset, emphasizing near-center distortion and radial pull.
/// Can be used for fish-eye or lens-style warp effects.
/// </summary>
/// <param name="texcoord">Input UV coordinates (0..1).</param>
/// <param name="strength">Overall intensity of the warp.</param>
/// <param name="shape">Aspect ratio influence (0 = no aspect correction, 1 = correct for screen aspect).</param>
/// <param name="screenSize">The size of the target screen.</param>
/// <returns>A float2 offset to apply to the original UV coordinates for the warp effect.</returns>
float2 WarpOffset(float2 texcoord, float strength, float shape, float2 screenSize)
{
    float2 centered = texcoord - 0.5;
    centered.y *= lerp(1.0, (screenSize.y / screenSize.x), shape);

    float r2 = dot(centered, centered);
    float2 offset = centered / max(r2, 1e-5) * strength * -0.25;

    offset.y *= lerp(1.0, (screenSize.x / screenSize.y), shape);
    return offset;
}

/// <summary>
/// Computes a complementary color by subtracting the input color from the maximum component value.
/// The result is a simple complementary approximation that preserves intensity per channel.
/// </summary>
/// <param name="color">Input RGB color vector (0..1 range recommended).</param>
/// <returns>Complementary RGB color vector.</returns>
float3 Complementary(float3 color)
{
    float3 n = max(color.r, max(color.g, color.b)).rrr;
    return n - color;
}

/// <summary>
/// Aligns a UV coordinate to the nearest texel boundary using the given texel size.
/// Useful for eliminating subpixel jitter or ensuring pixel-perfect sampling.
/// </summary>
/// <param name="texcoord">The UV coordinate to align.</param>
/// <param name="texelSize">
/// The size of a single texel in UV space (typically float2(1/width, 1/height)).
/// </param>
/// <returns>The UV coordinate snapped to the nearest texel grid.</returns>
float2 AlignToTexel(float2 texcoord, float2 texelSize)
{
    return texelSize * round(texcoord / texelSize);
}

/// <summary>
/// Computes a proportional UV offset or scale based on the screen size.
/// Optionally aligns the resulting value to the texel grid.
/// </summary>
/// <param name="texelSize">
/// Texel size in UV space (1/width, 1/height), typically from _ScreenParams.
/// </param>
/// <param name="screenSize">
/// The pixel resolution of the target (width, height).
/// </param>
/// <param name="align">
/// If true, the result is aligned to the texel grid to ensure pixel-perfect behavior.
/// </param>
/// <returns>
/// A proportional float2 scaled by screen size, optionally rounded to nearest texel.
/// </returns>
float2 ScreenProportional(float2 texelSize, float2 screenSize, bool align)
{
    float2 scale = screenSize.xy * Base_TexelSize.xy;
    float2 result = texelSize * scale;
    if (align)
    {
        result = AlignToTexel(result, texelSize);
    }
    return result;
}

/// <summary>
/// Checks whether the given UV coordinates are within the normalized [0,1] range.
/// </summary>
/// <param name="uv">The UV coordinates to test.</param>
/// <param name="cutoff">
/// If true, enforces the UV bounds check; if false, the function always returns true.
/// </param>
/// <returns>
/// True if the UV coordinates are within bounds or cutoff is false; false otherwise.
/// </returns>
bool UvInBounds(float2 uv, bool cutoff)
{
    return !cutoff || (0.0 <= uv.x && uv.x <= 1.0 && 0.0 < uv.y && uv.y <= 1.0);
}

/// <summary>
/// Fast cosine approximation using a 5th-order Taylor series.
/// Accurate for inputs in the range [0, PI].
/// </summary>
/// <param name="x">Input angle in radians.</param>
/// <returns>Approximate cosine of <paramref name="x"/>.</returns>
inline float CosApprox(float x)
{
    float s = PI / 2.0 - x;
    float s2 = s * s;
    float s3 = s2 * s;
    float s5 = s3 * s2;
    return s - s3 / 6.0 + s5 / 120.0;
}

/// <summary>
/// Fast sine approximation using a 6th-order Taylor series.
/// Accurate for inputs in the range [0, PI].
/// </summary>
/// <param name="x">Input angle in radians.</param>
/// <returns>Approximate sine of <paramref name="x"/>.</returns>
inline float SinApprox(float x)
{
    float s = x - PI / 2.0;
    float s2 = s * s;
    float s4 = s2 * s2;
    return 1.0 - s2 / 2.0 + s4 / 24.0;
}

/// <summary>
/// Blends a base color with a secondary color according to the specified mixing mode and strength.
/// </summary>
/// <param name="color">The original color to be modified.</param>
/// <param name="mixColor">The color used for blending or scaling.</param>
/// <param name="mixOption">
/// The mixing mode:
/// 0 = No change, returns <paramref name="color"/> unchanged.
/// 1 = Multiply blend: <c>mixColor * color</c> interpolated by <paramref name="strength"/>.
/// 2 = Scale by luminance: scales <paramref name="mixColor"/> by the ratio of luminances.
/// 3 = Additive blend: <c>mixColor * luminance + color</c> interpolated by <paramref name="strength"/>.
/// </param>
/// <param name="strength">Interpolation factor for the blending, typically in range 0..1.</param>
/// <param name="clamp">If true, clamps the base color luminance to 0..1 before computations.</param>
/// <returns>The resulting blended color as <c>float3</c>.</returns>
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

/// <summary>
/// Applies a 1D LUT (lookup texture) to the input color and blends it using the specified mixing mode and strength.
/// </summary>
/// <param name="lut">The lookup texture containing the color grading curve.</param>
/// <param name="color">The original color to modify.</param>
/// <param name="mode">
/// The mixing mode passed to <see cref="MixColor"/>:
/// 0 = No change, 1 = Multiply blend, 2 = Scale by luminance, 3 = Additive blend.
/// </param>
/// <param name="strength">The interpolation factor for blending, typically in range 0..1.</param>
/// <returns>The resulting color after LUT application and blending as <c>float3</c>.</returns>
float3 MixLut(TEXTURE2D(lut), float3 color, int mode, float strength)
{
    float luminance = saturate(Luminance(color));
    float3 lutColor = SAMPLE_TEXTURE2D(lut, sampler_LinearClampCustom, float2(luminance, 0.5)).rgb;

    color = MixColor(color, lutColor, mode, strength, true);

    return color;
}