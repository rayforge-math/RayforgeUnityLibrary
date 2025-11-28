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
    #define _TAA_Jitter                     _TaaJitter
    #define _TAA_JitterPrev                 _TaaJitterPrev

#elif defined(RAYFORGE_PIPELINE_URP)
    
    #define _TAA_MotionVectorTexture        _MotionVectorTexture
    #define sampler_TAA_MotionVectorTexture sampler_MotionVectorTexture

    #if !defined(_TaaJitter)
        #define _TAA_Jitter                 _TAA_Jitter
    #else
        #define _TAA_Jitter                 _TaaJitter
    #endif

    #if !defined(_TaaJitterPrev)
        #define _TAA_JitterPrev             _TAA_JitterPrev
    #else
        #define _TAA_JitterPrev             _TaaJitterPrev
    #endif

#else

    #define _TAA_MotionVectorTexture        _MotionVectorTexture
    #define sampler_TAA_MotionVectorTexture sampler_MotionVectorTexture

    #define _TAA_Jitter                     float2(0.0, 0.0)
    #define _TAA_JitterPrev                 float2(0.0, 0.0)

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
float4x4 _Rayforge_Matrix_Inv_VP;
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
/// <param name="uv">UV coordinates in [0,1].</param>
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
    float2 uv = currentUV - motionVector - (_TAA_Jitter - _TAA_JitterPrev);
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
/// Computes the per-channel mean and standard deviation from a 3�3 neighborhood
/// of history samples.  
///  
/// This statistical analysis is used for advanced temporal anti-aliasing
/// clamping (e.g., variance or clip-box clamping), allowing detection of
/// outlier history values that may cause ghosting.
/// </summary>
/// <param name="neighborhood">
/// A fixed array of 9 float3 color samples representing the 3�3 neighborhood
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
    for (int j = 0; j < 9; ++j)
    {
        float3 d = neighborhood[j] - mean;
        var += d * d;
    }
    var /= 9.0;
    stdDev = sqrt(var);
}

/// <summary>
/// Performs variance-based clip-box clamping on the history color using the mean and
/// standard deviation of a 3�3 neighborhood from the *current frame*.
/// This constrains the history color to a statistically plausible range,
/// reducing flicker and preventing extreme outliers before temporal accumulation.
/// </summary>
/// <param name="historyColor">
/// The input color from the history buffer.
/// </param>
/// <param name="currentNeighborhood">
/// A fixed array of 9 float3 samples representing the local 3�3 neighborhood
/// around the current pixel from the current frame.
/// </param>
/// <param name="scale">
/// Controls the width of the variance clip box.  
/// Typical values: 1.0�3.0  
/// Lower = more aggressive clamping (less ghosting, more flicker)  
/// Higher = looser clamping (smoother, more risk of ghosting).
/// </param>
/// <returns>
/// The history color clamped to [mean - stdDev * scale, mean + stdDev * scale].
/// </returns>
float3 VarianceClamp(float3 historyColor, float3 currentNeighborhood[9], float scale)
{
    float3 mean, stdDev;
    ComputeMeanAndStdDev9(currentNeighborhood, mean, stdDev);

    float3 minC = mean - stdDev * scale;
    float3 maxC = mean + stdDev * scale;

    return clamp(historyColor, minC, maxC);
}

/// <summary>
/// Performs luma-oriented clip-box clamping on the history color using the 3�3 neighborhood 
/// from the current frame. The clip box is aligned along the principal luma direction.
///
/// This is roughly the approach Unreal Engine uses for temporal AA:
/// see <see href="https://de45xmedrsdbp.cloudfront.net/Resources/files/TemporalAA_small-59732822.pdf#page=34">Unreal TAA Variance Clamping</see>.
/// </summary>
/// <param name="historyColor">
/// The input color of the history.
/// </param>
/// <param name="currentNeighborhood">
/// A fixed array of 9 float3 samples representing the local 3�3 neighborhood
/// around the current pixel from the current frame.
/// </param>
/// <param name="clipBoxScale">
/// Controls the width of the clip box.
/// Typical values: 1.0�3.0
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

    float meanLuma = dot(mean, float3(0.2126, 0.7152, 0.0722));

    // Compute the direction of largest difference in luminance (in color space) -> normalized gradient
    float3 lumaDir = (float3) 0;
    [unroll]
    for (int j = 0; j < 9; ++j)
    {
        float3 delta = currentNeighborhood[j] - mean;
        float lumaDelta = dot(currentNeighborhood[j], float3(0.2126, 0.7152, 0.0722)) - meanLuma;
        lumaDir += delta * lumaDelta;   // scale delta (direction) based on luminance delta
    }
    lumaDir = normalize(lumaDir + 1e-6);

    // project vector from history to mean onto axis along luminance gradient -> get amount of history along largest difference in lumiance
    float3 deltaHistory = historyColor - mean;
    float proj = dot(deltaHistory, lumaDir);
    
    // limit by standard deviation, scale lumaDir (normalized vector) by projected history difference
    float limit = length(stdDev) * clipBoxScale;
    float3 clampedDelta = clamp(proj, -limit, limit) * lumaDir;

    // return mean + the projected and scaled history offset
    return mean + clampedDelta;
}

/// <summary>
/// Clamps the current frame color to the min/max range defined by a 3�3 neighborhood
/// of the *current frame*.  
/// This prevents extreme differences before temporal accumulation.
/// </summary>
/// <param name="historyColor">
/// The input color of the history.
/// </param>
/// <param name="currentNeighborhood">
/// The 3�3 local neighborhood of the current pixel taken from the current frame.
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
/// depth rejection, motion-vector�based disocclusion, history weighting,
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
/// history color from the previous frame, correcting for TAA jitter.
/// </summary>
/// <param name="historyTexture">
/// The history color texture from the previous frame.
/// </param>
/// <param name="historySampler">
/// The sampler used to sample the history texture.
/// </param>
/// <param name="currentUV">
/// The UV coordinate of the current pixel in screen space [0..1].
/// </param>
/// <param name="motionVector">
/// Output: The motion vector retrieved from the motion vector buffer.
/// Unity motion vectors are based on unjittered projection matrices (as far as I know).
/// </param>
/// <param name="history">
/// Output: The reprojected history color from the previous frame, including
/// depth information typically stored in the alpha channel.
/// </param>
void SetupMotionVectorPipeline(TEXTURE2D_PARAM(historyTexture, historySampler), float2 currentUV, out float2 motionVector, out float4 history)
{
    motionVector = SAMPLE_TEXTURE2D_X(_TAA_MotionVectorTexture, sampler_TAA_MotionVectorTexture, currentUV).rg;
    history = SampleHistoryMotionVectors(historyTexture, historySampler, currentUV, motionVector);
}

/// <summary>
/// Reconstructs the world-space position of the current pixel using its UV
/// coordinate and depth value from the depth buffer.
/// </summary>
/// <param name="uv">
/// The UV coordinate of the current pixel in normalized screen space [0..1].
/// </param>
/// <param name="depth">
/// The non-linear clip-space depth value sampled from the depth buffer.
/// </param>
/// <returns>
/// The reconstructed world-space position of the pixel.
/// </returns>
float3 ReconstructWorldPos(float2 uv, float depth)
{
    float2 ndc = uv * 2.0 - 1.0;
    float4 posCS = float4(ndc, depth, 1.0);

    float4 posWS = mul(_Rayforge_Matrix_Inv_VP, posCS);
    posWS /= posWS.w;

    return posWS.xyz;
}

/// <summary>
/// Reconstructs the world-space position of the current pixel and samples the
/// history buffer using that world position. This enables world-space based
/// reprojection instead of screen-space UV reprojection.
/// </summary>
/// <param name="historyTexture">
/// The history buffer from the previous frame, storing color and depth
/// (depth typically in the alpha channel).
/// </param>
/// <param name="historySampler">
/// The sampler used to sample the history texture.
/// </param>
/// <param name="currentUV">
/// The UV coordinate of the current pixel in the current frame.
/// </param>
/// <param name="depth">
/// The non-linear depth value for the current pixel, sampled from the current
/// depth buffer.
/// </param>
/// <param name="history">
/// Output: The history sample reprojected using world-space position lookup.
/// Includes stored depth in the alpha channel.
/// </param>
void SetupWorldPosPipeline(TEXTURE2D_PARAM(historyTexture, historySampler), float2 currentUV, float depth, out float4 history)
{
    float3 worldPos = ReconstructWorldPos(currentUV, depth);
    history = SampleHistoryWorldPos(historyTexture, historySampler, worldPos);
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
/// Samples the TAA depth texture at the given UV coordinates and converts it
/// to linear 0�1 depth using the global z-buffer parameters.
/// </summary>
/// <param name="uv">UV coordinates to sample at.</param>
/// <returns>Linear depth in the range [0,1].</returns>
float SampleLinear01Depth(float2 uv)
{
    float rawDepth = SAMPLE_TEXTURE2D_X(_TAA_DepthTexture, sampler_TAA_DepthTexture, uv).r;
    return Linear01Depth(rawDepth, _ZBufferParams);
}

/// <summary>
/// Checks whether depth rejection should occur and updates the result
/// accordingly. If a depth mismatch is detected, the history is discarded
/// and the current color is used.
/// </summary>
/// <param name="currentUV">The UV coordinate of the current pixel.</param>
/// <param name="currentColor">The current frame's color at this pixel.</param>
/// <param name="currentDepth">The current frame's linear 0..1 depth value.</param>
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
bool CheckAndSetupDepthRejection(float2 currentUV, float3 currentColor, float currentDepth, float4 history, float threshold, inout float4 result)
{
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
/// Applies the selected temporal color clamping mode to a history color
/// based on the current 3x3 neighborhood.
/// </summary>
/// <param name="historyColor">The reprojected history color to potentially clamp.</param>
/// <param name="currentNeighborhood">The 3x3 neighborhood of current frame colors.</param>
/// <param name="clampMode">Clamping mode (0 = none, 1 = min/max, 2 = clip-box).</param>
/// <param name="scale">Scale factor for bounding box clamping, if required.</param>
/// <returns>The history color after applying the selected clamping mode.</returns>
float3 ApplyColorClamping(float3 historyColor, float3 currentNeighborhood[9], int clampMode, float scale)
{
    switch (clampMode)
    {
        default:
        case 0: // None
            break;
        case 1: // Min/Max Clamp
            historyColor = MinMaxClamp(historyColor, currentNeighborhood);
            break;
        case 2: // Variance Clamp
            historyColor = VarianceClamp(historyColor, currentNeighborhood, scale);
            break;
        case 3: // ClipBox Clamp
            historyColor = ClipBoxClamp(historyColor, currentNeighborhood, scale);
            break;
    }

    return historyColor;
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

    float currentDepth = SampleLinear01Depth(currentUV);

    if (params.depthRejection && CheckAndSetupDepthRejection(currentUV, currentColor, currentDepth, history, params.depthThreshold, result))
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
/// This variant requires a full 3�3 neighborhood of current-frame colors.
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
/// A 3�3 neighborhood of current-frame colors, indexed row-major.
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
    float currentDepth = SampleLinear01Depth(currentUV);

    if (params.depthRejection && CheckAndSetupDepthRejection(currentUV, currentColor, currentDepth, history, params.depthThreshold, result))
    {
        return result;
    }

    if(HasMotion(motionVector))
    {
        if(params.velocityDisocclusion)
        {
            SetupVelocityDisocclusion(motionVector, params.velocityThreshold, params.velocityScale, params.historyWeight);
        }

        history.rgb = ApplyColorClamping(history.rgb, currentNeighborhood, params.colorClampingMode, params.clipBoxScale);
    }

    result.rgb = Blend(currentColor, history.rgb, params.historyWeight);
    return result;
}

/// <summary>
/// Reprojects history using world-space reconstruction instead of motion vectors,
/// then blends the reprojected history color with the current pixel color.
///  
/// This variant assumes a mostly static world (no velocity-based disocclusion)
/// and relies on depth-based rejection to avoid ghosting when geometry changes,
/// moves across edges, or becomes newly visible.
/// 
/// Per-object motion is not taken into account.
/// </summary>
/// <param name="historyTexture">
/// The history color texture from the previous frame.  
/// Expected to store previous-frame depth in the alpha channel.
/// </param>
/// <param name="historySampler">
/// Sampler state used for sampling the history texture.
/// </param>
/// <param name="currentUV">
/// UV coordinate of the current pixel in normalized screen space [0..1].
/// </param>
/// <param name="currentColor">
/// The color computed for the current frame at this pixel (pre-TAA).
/// </param>
/// <param name="params">
/// Reprojection parameters controlling history weighting and depth rejection
/// behavior.
/// </param>
/// <returns>
/// The blended final color, combining current-frame color with  
/// world-reprojected history, or the current color alone if history is rejected.
/// </returns>
float4 BlendHistoryWorldPos(TEXTURE2D_PARAM(historyTexture, historySampler), float2 currentUV, float3 currentColor, ReprojectionParams params)
{
    float4 result = (float4) 0;

    float currentDepth = SampleLinear01Depth(currentUV);
    
    float4 history;
    SetupWorldPosPipeline(historyTexture, historySampler, currentUV, currentDepth, history);

    if (params.depthRejection && CheckAndSetupDepthRejection(currentUV, currentColor, currentDepth, history, params.depthThreshold, result))
    {
        return result;
    }

    result.rgb = Blend(currentColor, history.rgb, params.historyWeight);
    return result;
}

/// <summary>
/// Reprojects history using world-space reconstruction and applies optional
/// color-clamping using a 3�3 neighborhood from the current frame.
/// 
/// This variant is similar to the motion-vector version of history blending,
/// but uses world-space reprojection instead of stored motion vectors,
/// and therefore does not support velocity-based disocclusion.
/// 
/// Per-object motion is not taken into account.
/// </summary>
/// <param name="historyTexture">
/// The previous frame's history color texture.  
/// The alpha channel is expected to contain previous-frame depth.
/// </param>
/// <param name="historySampler">
/// Sampler state used when sampling the history texture.
/// </param>
/// <param name="currentUV">
/// UV coordinate of the current pixel in normalized screen space [0..1].
/// </param>
/// <param name="currentNeighborhood">
/// A 3�3 array of current-frame color samples centered at the current pixel.
/// Used for statistical color clamping (mean, variance, clip box, etc.).
/// </param>
/// <param name="params">
/// Reprojection settings controlling depth rejection, history weighting,
/// color-clamping mode, and clip-box parameters.
/// </param>
/// <returns>
/// The final TAA-filtered color for the pixel, combining the reprojected
/// history with the current frame's color after optional clamping.  
/// If history is rejected, returns the current color.
/// </returns>
float4 BlendHistoryWorldPos(TEXTURE2D_PARAM(historyTexture, historySampler), float2 currentUV, float3 currentNeighborhood[9], ReprojectionParams params)
{
    float4 result = (float4) 0;

    float currentDepth = SampleLinear01Depth(currentUV);
    
    float4 history;
    SetupWorldPosPipeline(historyTexture, historySampler, currentUV, currentDepth, history);

    float3 currentColor = currentNeighborhood[4];


    if (params.depthRejection && CheckAndSetupDepthRejection(currentUV, currentColor, currentDepth, history, params.depthThreshold, result))
    {
        return result;
    }

    history.rgb = ApplyColorClamping(history.rgb, currentNeighborhood, params.colorClampingMode, params.clipBoxScale);

    result.rgb = Blend(currentColor, history.rgb, params.historyWeight);
    return result;
}