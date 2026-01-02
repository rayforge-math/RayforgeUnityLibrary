#pragma once

// ============================================================================
// CustomUnityLibrary - Lighting Shader Include
// Author: Matthew
// Description: pipeline independant HLSL utilities for Unity
// ============================================================================

// ============================================================================
// 1. Defines/Macros
// ============================================================================

#define PI_4 (4.0 * PI)

// ============================================================================
// 2. Utility Functions
// ============================================================================

/// @brief Computes the exact Henyey-Greenstein phase function.
/// 
/// Models anisotropic scattering of light in a participating medium.
/// @param cosTheta Cosine of the angle between the ray and light directions.
/// @param g Anisotropy factor of the medium (-1 = backward, 0 = isotropic, 1 = forward).
/// @return Fraction of light scattered along the given direction.
float HenyeyGreensteinPhase(float cosTheta, float g)
{
    float g2 = g * g;
    return (1.0 - g2) / (pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5) * PI_4);
}

/// @brief Computes an approximate, cheaper version of the Henyey-Greenstein phase function.
/// 
/// Useful for performance-sensitive volumetric shaders.
/// @param cosTheta Cosine of the angle between the ray and light directions.
/// @param g Anisotropy factor (-1..1).
/// @return Approximate fraction of light scattered along the given direction.
float HenyeyGreensteinPhaseApprox(float cosTheta, float g)
{
    float g2 = g * g;
    return (1.0 - g2) / (pow(1.0 - g * cosTheta, 2.0) * PI_4);
}

/// @brief Computes the exact Henyey-Greenstein scattering for a ray in a volumetric medium.
///
/// The scattering fraction is computed using the Henyey-Greenstein phase function.
/// The cosine of the scattering angle θ is defined by the dot product:
///
///     cos(θ) = (a ⋅ b) / (|a| * |b|)
///
/// where:
/// - a = rayDir   (direction of the current ray)
/// - b = lightDir (direction from the current sample point toward the light source)
///
/// @note Both vectors must be normalized.
///       With |a| = |b| = 1, the equation simplifies to:
///
///           cos(θ) = dot(rayDir, lightDir)
///
/// @param rayDir Normalized ray direction (from the sample along the view ray).
/// @param lightDir Normalized direction from the sample toward the light source.
/// @param g Anisotropy factor (-1 = backward, 0 = isotropic, 1 = forward).
/// @return Fraction of light scattered along rayDir according to the Henyey-Greenstein phase function.
float HenyeyGreensteinScattering(float3 rayDir, float3 lightDir, float g)
{
    float cosTheta = dot(rayDir, lightDir);
    return HenyeyGreensteinPhase(cosTheta, g);
}

/// @brief Computes an approximate Henyey-Greenstein scattering for performance-sensitive shaders.
///
/// Uses the same physical principle as the exact formulation:
///
///     cos(θ) = (a ⋅ b) / (|a| * |b|)
///
/// With normalized vectors:
/// - a = rayDir
/// - b = lightDir
///
/// this simplifies to:
///
///     cos(θ) = dot(rayDir, lightDir)
///
/// @param rayDir Normalized ray direction.
/// @param lightDir Normalized direction toward the light source.
/// @param g Anisotropy factor (-1..1).
/// @return Approximate fraction of light scattered along rayDir.
float HenyeyGreensteinScatteringApprox(float3 rayDir, float3 lightDir, float g)
{
    float cosTheta = dot(rayDir, lightDir);
    return HenyeyGreensteinPhaseApprox(cosTheta, g);
}