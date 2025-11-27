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
/// standard deviation of a 3×3 neighborhood from the *current frame*.
/// This limits the current color to a statistically plausible range,
/// reducing flicker and preventing extreme outliers before temporal accumulation.
/// </summary>
/// <param name="historyColor">
/// The input color of the history.
/// </param>
/// <param name="currentNeighborhood">
/// A fixed array of 9 float3 samples representing the local 3×3 neighborhood
/// around the current pixel from the *current frame*.
/// </param>
/// <param name="clipBoxScale">
/// Controls the width of the clip box.  
/// Typical values: 1.0–3.0  
/// Lower = more aggressive clamping (less ghosting, more flicker)  
/// Higher = smoother but more ghosting risk.
/// </param>
/// <returns>
/// The clamped color of the current pixel.
/// </returns>
float3 ClipBoxClamp(float3 historyColor, float3 currentNeighborhood[9], float clipBoxScale)
{
    float3 mean, stdDev;
    ComputeMeanAndStdDev9(currentNeighborhood, mean, stdDev);

    float3 minC = mean - stdDev * clipBoxScale;
    float3 maxC = mean + stdDev * clipBoxScale;

    return clamp(historyColor, minC, maxC);
}

/// <summary>
/// Clamps the current frame color to the min/max range defined by a 3×3 neighborhood
/// of the *current frame*.  
/// This prevents extreme differences before temporal accumulation.
/// </summary>
/// <param name="historyColor">
/// The input color of the history.
/// </param>
/// <param name="currentNeighborhood">
/// The 3×3 local neighborhood of the current pixel taken from the current frame.
/// </param>
/// <returns>
/// The current color clamped to the min/max bounding box of the local neighborhood.
/// </returns>
float3 MinMaxClamp(float3 historyColor, float3 currentNeighborhood[9])
{
    float3 minColor = float3(1e9, 1e9, 1e9);
    float3 maxColor = float3(-1e9, -1e9, -1e9);

    [unroll]
    for (int i = 0; i < 9; ++i)
    {
        minColor = min(minColor, currentNeighborhood[i]);
        maxColor = max(maxColor, currentNeighborhood[i]);
    }

    return clamp(historyColor, minColor, maxColor);
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

/// <summary>
/// Parameter block controlling temporal reprojection behavior, including 
/// depth rejection, motion-vector–based disocclusion, history weighting,
/// and optional neighborhood-based color clamping.
/// </summary>
/// <remarks>
/// Intentionally made to be 16 byte aligned, fitting within 2 4-component 32 bit vector registers.
/// </remarks>
struct ReprojectionParams
{
    bool depthRejection;
    float depthThreshold;
    bool velocityDisocclusion;
    float velocityThreshold;
    float velocityScale;
    float historyWeight;
    int colorClampingMode;
    float clipBoxScale;
};

/// <summary>
/// Samples the motion vector at the current UV and fetches the reprojected
/// history color from the previous frame.
/// </summary>
/// <param name="historyTexture">The history color texture from the previous frame.</param>
/// <param name="historySampler">The sampler used to sample the history texture.</param>
/// <param name="currentUV">The UV coordinate of the current pixel.</param>
/// <param name="motionVector">
/// Output: The motion vector retrieved from the motion vector buffer.
/// </param>
/// <param name="history">
/// Output: The reprojected history color, including depth stored
/// in the alpha channel.
/// </param>
void SetupMotionVectorPipeline(TEXTURE2D_PARAM(historyTexture, historySampler), float2 currentUV, out float2 motionVector, out float4 history)
{
    motionVector = SAMPLE_TEXTURE2D_X(_TAA_MotionVectorTexture, sampler_TAA_MotionVectorTexture, currentUV).rg;
    history = SampleHistoryMotionVectors(historyTexture, historySampler, currentUV, motionVector);
}

/// <summary>
/// Determines whether the given motion vector indicates noticeable motion.
/// </summary>
/// <param name="motionVector">The motion vector to test.</param>
/// <returns>
/// True if the magnitude of the motion vector exceeds a small threshold;
/// otherwise false.
/// </returns>
bool HasMotion(float2 motionVector)
{
    return dot(motionVector, motionVector) > 1e-6;
}

/// <summary>
/// Checks whether depth rejection should occur and updates the result
/// accordingly. If a depth mismatch is detected, the history is discarded
/// and the current color is used.
/// </summary>
/// <param name="currentUV">The UV coordinate of the current pixel.</param>
/// <param name="currentColor">The current frame's color at this pixel.</param>
/// <param name="history">
/// The history color, where the alpha channel contains previous-frame depth.
/// </param>
/// <param name="threshold">
/// Depth threshold used to determine whether the history is valid.
/// </param>
/// <param name="result">
/// In/out: On rejection, this is filled with the current color and updated depth.
/// </param>
/// <returns>
/// True if depth rejection occurred; otherwise false.
/// </returns>
bool CheckAndSetupDepthRejection(float2 currentUV, float3 currentColor, float4 history, float threshold, inout float4 result)
{
    float currentDepth = SAMPLE_TEXTURE2D_X(_TAA_DepthTexture, sampler_TAA_DepthTexture, currentUV).r;
    currentDepth = Linear01Depth(currentDepth, _ZBufferParams);

    float prevDepth = history.a;
    result.a = currentDepth;

    if (DepthReject(currentDepth, prevDepth, threshold))
    {
        result.rgb = currentColor;
        return true;
    }
    return false;
}

/// <summary>
/// Reduces the history weight if the pixel is likely disoccluded,
/// based on motion vector magnitude.
/// </summary>
/// <param name="motionVector">The pixel's motion vector.</param>
/// <param name="threshold">
/// Motion magnitude threshold for detecting disocclusion.
/// </param>
/// <param name="scale">
/// Additional scale factor to amplify or diminish disocclusion sensitivity.
/// </param>
/// <param name="historyWeight">
/// In/out: The blending weight applied to the history color.
/// Reduced when disocclusion is detected.
/// </param>
void SetupVelocityDisocclusion(float2 motionVector, float threshold, float scale, inout float historyWeight)
{
    float disocclusion = VelocityMagnitudeDisocclusion(motionVector, threshold, scale);
    historyWeight *= (1.0 - disocclusion);
}

/// <summary>
/// Reprojects and blends the history color for the current pixel using motion vectors
/// and optional rejection heuristics. This variant uses only the current pixel color,
/// without neighborhood-based color clamping.
/// </summary>
/// <param name="historyTexture">
/// The history color texture from the previous frame.
/// </param>
/// <param name="historySampler">
/// The sampler used to sample the history texture.
/// </param>
/// <param name="currentUV">
/// The UV coordinate of the current pixel.
/// </param>
/// <param name="currentColor">
/// The current-frame color at this pixel.
/// </param>
/// <param name="params">
/// Reprojection settings controlling depth rejection, velocity disocclusion,
/// history weighting, and optional color clamping mode.
/// </param>
/// <returns>
/// The blended color result, combining the current color and reprojected history.
/// </returns>
float4 BlendHistoryMotionVectors(TEXTURE2D_PARAM(historyTexture, historySampler), float2 currentUV, float3 currentColor, ReprojectionParams params)
{
    float4 result = (float4) 0;

    float2 motionVector;
    float4 history;
    SetupMotionVectorPipeline(historyTexture, historySampler, currentUV, motionVector, history);

    if (params.depthRejection && CheckAndSetupDepthRejection(currentUV, currentColor, history, params.depthThreshold, result))
    {
        return result;
    }

    if(HasMotion(motionVector) && params.velocityDisocclusion)
    {
        SetupVelocityDisocclusion(motionVector, params.velocityThreshold, params.velocityScale, params.historyWeight);
    }

    result.rgb = Blend(currentColor, history.rgb, params.historyWeight);
    return result;
}

/// <summary>
/// Reprojects and blends the history color using motion vectors, with optional
/// depth rejection, velocity disocclusion, and neighborhood-based color clamping.
/// This variant requires a full 3×3 neighborhood of current-frame colors.
/// </summary>
/// <param name="historyTexture">
/// The history color texture from the previous frame.
/// </param>
/// <param name="historySampler">
/// The sampler used to sample the history texture.
/// </param>
/// <param name="currentUV">
/// The UV coordinate of the current pixel.
/// </param>
/// <param name="currentNeighborhood">
/// A 3×3 neighborhood of current-frame colors, indexed row-major.
/// Element [4] must contain the current pixel's color.
/// </param>
/// <param name="params">
/// Reprojection settings controlling depth rejection, velocity disocclusion,
/// history weighting, and the color clamping mode (None, MinMax, ClipBox).
/// </param>
/// <returns>
/// The blended color result after reprojection, clamping, and temporal filtering.
/// </returns>
float4 BlendHistoryMotionVectors(TEXTURE2D_PARAM(historyTexture, historySampler), float2 currentUV, float3 currentNeighborhood[9], ReprojectionParams params)
{
    float4 result = (float4) 0;

    float2 motionVector;
    float4 history;
    SetupMotionVectorPipeline(historyTexture, historySampler, currentUV, motionVector, history);

    float3 currentColor = currentNeighborhood[4];

    if (params.depthRejection && CheckAndSetupDepthRejection(currentUV, currentColor, history, params.depthThreshold, result))
    {
        return result;
    }

    if(HasMotion(motionVector))
    {
        if(params.velocityDisocclusion)
        {
            SetupVelocityDisocclusion(motionVector, params.velocityThreshold, params.velocityScale, params.historyWeight);
        }

        switch(params.colorClampingMode)
        {
        default:
        case 0:
            break;
        case 1:
            history.rgb = MinMaxClamp(history.rgb, currentNeighborhood);
            break;
        case 2:
            history.rgb = ClipBoxClamp(history.rgb, currentNeighborhood, params.clipBoxScale);
            break;
        }
    }

    result.rgb = Blend(currentColor, history.rgb, params.historyWeight);
    return result;
}