#pragma once

// ============================================================================
// CustomUnityLibrary - Common Shader Include
// Author: Matthew
// Description: blue noise functionality
// ============================================================================

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// ============================================================================
// 1. Inputs
// ============================================================================

TEXTURE2D(_Rayforge_BlueNoise);
SAMPLER(sampler_Rayforge_BlueNoise);
float4 _Rayforge_BlueNoise_TexelSize;

// ============================================================================
// 2. Utility Functions
// ============================================================================

float SampleBlueNoise(float2 screenUV, float2 screenSize)
{
    screenUV.x *= screenSize.x / screenSize.y;

    return SAMPLE_TEXTURE2D(_Rayforge_BlueNoise, sampler_Rayforge_BlueNoise, screenUV).r;
}
