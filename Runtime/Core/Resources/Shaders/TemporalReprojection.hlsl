#pragma once

// ============================================================================
// CustomUnityLibrary - Temporal Reprojection Shader Include
// Author: Matthew
// Description: pipeline independant HLSL utilities for Unity
// ============================================================================

// ============================================================================
// 1. Defines/Macros
// ============================================================================

#if defined(RAYFORGE_PIPELINE_HDRP)
    #define _TAA_MotionVectorTexture        _CameraMotionVectorsTexture
    #define sampler_TAA_MotionVectorTexture sampler_CameraMotionVectorsTexture
#elif defined(RAYFORGE_PIPELINE_URP)
    #define _TAA_MotionVectorTexture        _MotionVectorTexture
    #define sampler_TAA_MotionVectorTexture sampler_MotionVectorTexture
#else
    #define _TAA_MotionVectorTexture        _MotionVectorTexture
    #define sampler_TAA_MotionVectorTexture sampler_MotionVectorTexture
#endif

#define _TAA_DepthTexture               _CameraDepthTexture
#define sampler_TAA_DepthTexture        sampler_CameraDepthTexture

// ============================================================================
// 2. Includes
// ============================================================================

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// ============================================================================
// 3. Variables
// ============================================================================
float4x4 _Rayforge_Matrix_Prev_VP;

#if !defined(RAYFORGE_DEPTH_TEXTURE)
#define RAYFORGE_DEPTH_TEXTURE
TEXTURE2D_X(_TAA_DepthTexture);
SAMPLER(sampler_TAA_DepthTexture);
#endif

#if !defined(RAYFORGE_MOTIONVECTOR_TEXTURE)
#define RAYFORGE_MOTIONVECTOR_TEXTURE
TEXTURE2D_X(_TAA_MotionVectorTexture);
SAMPLER(sampler_TAA_MotionVectorTexture);
#endif

// ============================================================================
// 4. Utility Functions
// ============================================================================

/// <summary>
/// Samples a history texture at the given UV coordinates.
/// Returns <c>(0,0,0,0)</c> if the UVs are outside [0,1].
/// </summary>
/// <param name="historyTexture">The history texture to sample.</param>
/// <param name="historySampler">Sampler state for the texture.</param>
/// <param name="uv">UV coordinates in [0,1] space.</param>
/// <returns>The sampled color from the history texture, or zero if UV is invalid.</returns>
float4 SampleHistory(TEXTURE2D_PARAM(historyTexture, historySampler), float2 uv)
{
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
        return float4(0,0,0,0);
    
    return SAMPLE_TEXTURE2D(historyTexture, historySampler, uv);
}

/// <summary>
/// Samples a history texture using motion vectors to offset the current UV coordinates.
/// This is useful for temporal reprojection of screen-space effects.
/// </summary>
/// <param name="historyTexture">The history texture to sample.</param>
/// <param name="historySampler">Sampler state for the texture.</param>
/// <param name="currentUV">Current frame UV coordinates.</param>
/// <param name="motionVector">Motion vector to reproject the UV into the previous frame. Usually retrieved from the combined camera + object motion.</param>
/// <returns>The sampled color from the history texture at the reprojected position.</returns>
float4 SampleHistoryMotionVectors(TEXTURE2D_PARAM(historyTexture, historySampler), float2 currentUV, float2 motionVector)
{
    float2 uv = currentUV + motionVector;
    return SampleHistory(historyTexture, historySampler, uv);
}

/// <summary>
/// Samples a history texture by projecting a world-space position into the previous frame.
/// Converts the world position to clip space using the previous view-projection matrix,
/// performs perspective division, and converts to UV coordinates for sampling.
/// </summary>
/// <param name="historyTexture">The history texture to sample.</param>
/// <param name="historySampler">Sampler state for the texture.</param>
/// <param name="worldPos">World-space position to reproject into the previous frame.</param>
/// <returns>The sampled color from the history texture, or zero if the projected pixel is invalid.</returns>
float4 SampleHistoryWorldPos(TEXTURE2D_PARAM(historyTexture, historySampler), float3 worldPos)
{
    float4 clipPrev = mul(_Rayforge_Matrix_Prev_VP, float4(worldPos, 1.0));

    // If w is very small or negative, pixel is invalid (was behind camera)
    if (clipPrev.w <= 0.0f)
        return float4(0,0,0,0);

    float2 ndcPrev = clipPrev.xy / clipPrev.w;
    float2 uv = ndcPrev * 0.5 + 0.5;

    return SampleHistory(historyTexture, historySampler, uv);
}

/// <summary>
/// Clamps a previous frame color to the range defined by a 3x3 neighborhood of current frame samples.
/// This helps to prevent history samples from introducing extreme outliers that cause ghosting or smearing.
/// </summary>
/// <param name="previous">The color sampled from the previous frame's history buffer.</param>
/// <param name="neighborhood">
/// An array of 9 colors representing the current frame's 3x3 neighborhood around the pixel.
/// The previous color will be clamped to the min/max bounds of these values.
/// </param>
/// <returns>
/// The previous color clamped to the bounding box defined by the neighborhood.
/// </returns>
float3 ClampPreviousColor(float3 previous, float3 neighborhood[9])
{
    float3 minColor = float3(1e9, 1e9, 1e9);
    float3 maxColor = float3(-1e9, -1e9, -1e9);

    [unroll]
    for (int i = 0; i < 9; ++i)
    {
        minColor = min(minColor, neighborhood[i]);
        maxColor = max(maxColor, neighborhood[i]);
    }

    return clamp(previous, minColor, maxColor);
}

/// <summary>
/// Determines whether a previous frame sample should be rejected based on depth difference.
/// Useful to avoid blending history across surfaces at different depths, reducing ghosting.
/// </summary>
/// <param name="currentDepth">Depth of the current pixel in view or linear depth space.</param>
/// <param name="previousDepth">Depth of the corresponding pixel in the history buffer.</param>
/// <param name="threshold">Maximum allowed depth difference before rejecting the history sample.</param>
/// <returns>
/// <c>true</c> if the depth difference is within the threshold (sample is valid);
/// <c>false</c> if the sample should be rejected.
/// </returns>
bool DepthReject(float currentDepth, float previousDepth, float threshold)
{
    return abs(currentDepth - previousDepth) <= threshold;
}

/// <summary>
/// Computes a disocclusion factor based on the difference in velocity between the current and previous frames.
/// This is used to reduce ghosting when objects move quickly or appear/disappear.
/// </summary>
/// <param name="currentVelocityUV">Velocity of the current pixel in UV space.</param>
/// <param name="prevVelocityUV">Velocity of the corresponding pixel in the previous frame in UV space.</param>
/// <param name="epsilon">Minimum velocity difference considered as disocclusion.</param>
/// <param name="scale">Scaling factor to control the sharpness of disocclusion response.</param>
/// <returns>
/// A value between 0 and 1 indicating the degree of disocclusion:
/// 0 = no disocclusion (history is valid), 1 = full disocclusion (ignore history).
/// </returns>
float VelocityDisocclusion(float2 currentVelocityUV, float2 prevVelocityUV, float epsilon, float scale)
{
    float diffLength = length(prevVelocityUV - currentVelocityUV);
    return saturate((diffLength - epsilon) * scale);
}

/// <summary>
/// Blends the current frame color with a previous frame sample using a specified history weight.
/// </summary>
/// <param name="current">Color of the current frame.</param>
/// <param name="previous">Color sampled from the previous frame (possibly clamped or depth-rejected).</param>
/// <param name="historyWeight">Weight of the history sample in the final blend. Range [0,1].</param>
/// <returns>The blended color.</returns>
float3 Blend(float3 current, float3 previous, float historyWeight)
{
    return lerp(current, previous, historyWeight);
}

struct ReprojectionParams
{
    bool colorClamping;
    bool depthRejection;
    float depthThreshold;
    bool velocityDisocclusion;
    float velocityEpsilon;
    float velocityScale;
    float historyWeight;
};

float4 BlendHistoryMotionVectors(TEXTURE2D_PARAM(historyTexture, historySampler), float2 currentUV, float3 currentColor, ReprojectionParams params)
{
    float4 result = (float4) 0;

    float2 motionVector = SAMPLE_TEXTURE2D_X(_TAA_MotionVectorTexture, sampler_TAA_MotionVectorTexture, currentUV).rg;
    float4 history = SampleHistoryMotionVectors(historyTexture, historySampler, currentUV, motionVector);
    
    if (params.depthRejection)
    {
        float currentDepth = SAMPLE_TEXTURE2D_X(_TAA_DepthTexture, sampler_TAA_DepthTexture, currentUV).r;
        float prevDepth = history.a;
        result.a = currentDepth;
        if(!DepthReject(currentDepth, prevDepth, params.depthThreshold))
            history.rgb = currentColor;
    }

    result.rgb = Blend(currentColor, history.rgb, params.historyWeight);
    return result;
}