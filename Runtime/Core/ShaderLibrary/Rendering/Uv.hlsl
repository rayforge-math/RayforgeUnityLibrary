#pragma once

// ============================================================================
// 1. Utility Functions
// ============================================================================

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