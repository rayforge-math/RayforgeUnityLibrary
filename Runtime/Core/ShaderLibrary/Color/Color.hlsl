#pragma once

// ============================================================================
// 1. Includes
// ============================================================================

#include "Packages/eu.rayforge.unitylibrary/Runtime/Core/ShaderLibrary/Color/ColorMix.hlsl"

// ============================================================================
// 2. Utility Functions
// ============================================================================

/// @brief Computes a complementary color by subtracting the input color from the maximum component value.
/// The result is a simple complementary approximation that preserves intensity per channel.
/// @param color Input RGB color vector (0..1 range recommended).
/// @return Complementary RGB color vector.
float3 Complementary(float3 color)
{
    float3 n = max(color.r, max(color.g, color.b)).rrr;
    return n - color;
}

