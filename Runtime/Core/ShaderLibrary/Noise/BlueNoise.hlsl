#pragma once

// ============================================================================
// CustomUnityLibrary - Common Shader Include
// Author: Matthew
// Description: blue noise functionality
// ============================================================================

// ============================================================================
// 1. Includes
// ============================================================================

#include "Packages/eu.rayforge.unitylibrary/Runtime/Core/ShaderLibrary/Noise/Params.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// ============================================================================
// 2. Utility Functions
// ============================================================================

/// @brief Samples a blue-noise texture in screen space.
/// @param screenUV UV coordinate in screen space (0�1)
/// @param screenSize Screen resolution in pixels
/// @return The red channel value of the sampled blue-noise texture
float SampleBlueNoise(float2 screenUV, float2 screenSize)
{
    screenUV.x *= screenSize.x / screenSize.y;
    return SAMPLE_TEXTURE2D(_Rayforge_BlueNoise, sampler_Rayforge_BlueNoise, screenUV).r;
}
