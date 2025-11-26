#pragma once

// ============================================================================
// CustomUnityLibrary - Temporal Reprojection Shader Include
// Author: Matthew
// Description: pipeline independant HLSL utilities for Unity
// ============================================================================

// ============================================================================
// 1. Defines/Macros
// ============================================================================

#pragma multi_compile __ TAA_USE_NEIGHBORHOOD_CLAMP

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

CBUFFER_START(UnityPerCamera)
float4x4 _Rayforge_Matrix_Prev_VP;
CBUFFER_END

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
    float2 uv = currentUV - motionVector;
    return SampleHistory(historyTexture, historySampler, uv);
}

/// <summary>
/// Samples a history texture by projecting a world-space position into the previous frame.
/// Converts the world position to clip space using the previous view-projection matrix,
/// performs perspective division, and converts to UV coordinates for sampling.
/// </summary>
/// <param name="historyTexture">The history texture to sample.</param>
/// <param name="historySampler">Sampler state for the texture.</param>
/// <param name="worldPos">World-space position to reproject into the previous frame. Usually reconstructed from depth buffer.</param>
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
/// Computes the per-channel mean and standard deviation from a 3×3 neighborhood
/// of history samples.  
///  
/// This statistical analysis is used for advanced temporal anti-aliasing
/// clamping (e.g., variance or clip-box clamping), allowing detection of
/// outlier history values that may cause ghosting.
/// </summary>
/// <param name="neighborhood">
/// A fixed array of 9 float3 color samples representing the 3×3 neighborhood
/// around the reprojected history pixel.
/// </param>
/// <param name="mean">
/// Output: The per-channel arithmetic mean of the neighborhood.
/// </param>
/// <param name="stdDev">
/// Output: The per-channel standard deviation, describing how much variation
/// exists in the neighborhood. Higher values indicate more variance.
/// </param>
void ComputeMeanAndStdDev9(in float3 neighborhood[9], out float3 mean, out float3 stdDev)
{
    mean = float3(0, 0, 0);
    [unroll]
    for (int i = 0; i < 9; ++i)
        mean += neighborhood[i];
    mean /= 9.0;

    float3 var = float3(0, 0, 0);
    [unroll]
    for (int i = 0; i < 9; ++i)
    {
        float3 d = neighborhood[i] - mean;
        var += d * d;
    }
    var /= 9.0;
    stdDev = sqrt(var);
}

/// <summary>
/// Performs clip-box clamping on the current frame color using the mean and
/// standard deviation of a 3×3 neighborhood from the reprojected history.
/// This limits the current color to a statistically plausible range based on history,
/// reducing ghosting and preventing extreme temporal deviations.
/// </summary>
/// <param name="currentColor">
/// The current frame color that should be validated and potentially clamped.
/// </param>
/// <param name="historyNeighborhood">
/// A fixed array of 9 float3 samples representing the surrounding reprojected
/// history pixels used to compute the statistical clip region.
/// </param>
/// <param name="clipBoxScale">
/// A scalar controlling the size of the clip box.  
/// Typical values range from 1.0 to 3.0:  
/// Lower values = more aggressive clamping (less ghosting, more flicker)  
/// Higher values = looser clamping (more stability, more ghosting risk)
/// </param>
/// <returns>
/// The clamped current color, ensuring it fits within the computed statistical
/// boundaries of the history neighborhood.
/// </returns>
float3 ClipBoxClampCurrent(float3 currentColor, float3 historyNeighborhood[9], float clipBoxScale)
{
    float3 mean, stdDev;
    ComputeMeanAndStdDev9(historyNeighborhood, mean, stdDev);

    float3 minC = mean - stdDev * clipBoxScale;
    float3 maxC = mean + stdDev * clipBoxScale;

    return clamp(currentColor, minC, maxC);
}

/// <summary>
/// Clamps the current frame color to the min/max range defined by a 3×3 neighborhood
/// of reprojected history pixels. Prevents extreme deviations in temporal blending.
/// </summary>
/// <param name="currentColor">The current frame color to clamp.</param>
/// <param name="historyNeighborhood">
/// An array of 9 colors representing the local neighborhood of reprojected history pixels.
/// </param>
/// <returns>
/// The current color clamped to the bounding box defined by the history neighborhood.
/// </returns>
float3 MinMaxClampCurrent(float3 currentColor, float3 historyNeighborhood[9])
{
    float3 minColor = float3(1e9, 1e9, 1e9);
    float3 maxColor = float3(-1e9, -1e9, -1e9);

    [unroll]
    for (int i = 0; i < 9; ++i)
    {
        minColor = min(minColor, historyNeighborhood[i]);
        maxColor = max(maxColor, historyNeighborhood[i]);
    }

    return clamp(currentColor, minColor, maxColor);
}

/// <summary>
/// Determines whether a previous frame sample should be rejected based on depth difference.
/// Useful to avoid blending history across surfaces at different depths, reducing ghosting.
/// </summary>
/// <param name="currentDepth">Depth of the current pixel in view or linear depth space.</param>
/// <param name="previousDepth">Depth of the corresponding pixel in the history buffer.</param>
/// <param name="threshold">Maximum allowed depth difference before rejecting the history sample.</param>
/// <returns>
/// <c>true</c> if the sample should be rejected;
/// <c>false</c> if the depth difference is within the threshold (sample is valid).
/// </returns>
bool DepthReject(float currentDepth, float previousDepth, float threshold)
{
    return abs(currentDepth - previousDepth) > threshold;
}

/// <summary>
/// Computes a disocclusion factor based solely on the motion magnitude of the current frame.
/// High-motion pixels are treated as increasingly invalid to reduce ghosting, without needing previous frame motion.
/// </summary>
/// <param name="velocityUV">Motion vector of the current pixel in UV space (current frame).</param>
/// <param name="threshold">Velocity magnitude above which history starts to be ignored.</param>
/// <param name="scale">Scaling factor controlling how quickly the disocclusion ramps from 0 to 1.</param>
/// <returns>
/// A value in [0,1] representing the disocclusion factor:
/// 0 = history fully valid, 1 = history fully ignored.
/// </returns>
float VelocityMagnitudeDisocclusion(float2 velocityUV, float threshold, float scale)
{
    float speed = length(velocityUV);
    return saturate((speed - threshold) * scale);
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
    bool depthRejection;
    float depthThreshold;
    bool velocityDisocclusion;
    float velocityThreshold;
    float velocityScale;
    float historyWeight;
#if defined(TAA_USE_NEIGHBORHOOD_CLAMP)
    bool colorClampingMode;
    float clipBoxScale;
#endif
};

void SampleHistoryNeighborhood(TEXTURE2D_PARAM( historyTexture, historySampler), float2 historyTexelSize, float2 texcoord, out float3 neighborhood[9], out float4 centreHistory)
{
    static const float2 offs[9] =
    {
        float2(-1, -1), float2(0, -1), float2(1, -1),
        float2(-1, 0), float2(0, 0), float2(1, 0),
        float2(-1, 1), float2(0, 1), float2(1, 1)
    };

    [unroll]
    for (int i = 0; i < 9; ++i)
    {
        float2 uv = texcoord + offs[i] * historyTexelSize;
        float4 sample = SAMPLE_TEXTURE2D(historyTexture, historySampler, uv);
        
        neighborhood[i] = sample.rgb;
        if (i == 4)
        {
            centreHistory = sample;
        }
    }
}

float4 BlendHistoryMotionVectors(TEXTURE2D_PARAM(historyTexture, historySampler), float2 historyTexelSize, float2 currentUV, float3 currentColor, ReprojectionParams params)
{
    float4 result = (float4) 0;

    float2 motionVector = SAMPLE_TEXTURE2D_X(_TAA_MotionVectorTexture, sampler_TAA_MotionVectorTexture, currentUV).rg;

    float4 history;
#if defined(TAA_USE_NEIGHBORHOOD_CLAMP)
    float3 neighborhood[9];
    SampleHistoryNeighborhood(historyTexture, historySampler, historyTexelSize, currentUV, neighborhood, history);
#else
    history = SampleHistoryMotionVectors(historyTexture, historySampler, currentUV, motionVector);
#endif
    

    if (params.depthRejection)
    {
        float currentDepth = SAMPLE_TEXTURE2D_X(_TAA_DepthTexture, sampler_TAA_DepthTexture, currentUV).r;
        currentDepth = Linear01Depth(currentDepth, _ZBufferParams);

        float prevDepth = history.a;
        result.a = currentDepth;

        if(DepthReject(currentDepth, prevDepth, params.depthThreshold))
        {
            result.rgb = currentColor;
            return result;
        }
    }

    if (params.velocityDisocclusion)
    {
        float disocclusion = VelocityMagnitudeDisocclusion(motionVector, params.velocityThreshold, params.velocityScale);
        params.historyWeight *= (1.0 - disocclusion);
    }

#if defined(TAA_USE_NEIGHBORHOOD_CLAMP)
    switch(params.colorClampingMode)
    {
    default:
    case 0:
        break;
    case 1:
        currentColor = MinMaxClampCurrent(currentColor, neighborhood);
        break;
    case 2:
        currentColor = ClipBoxClampCurrent(currentColor, neighborhood, params.clipBoxScale);
        break;
    }
#endif

    result.rgb = Blend(currentColor, history.rgb, params.historyWeight);
    return result;
}