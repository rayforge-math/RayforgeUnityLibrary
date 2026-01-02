#pragma once

CBUFFER_START(_Rayforge_TaaCamera)
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

/// @brief Parameter block controlling temporal reprojection behavior, including
/// depth rejection, motion-vector-based disocclusion, history weighting,
/// and optional neighborhood-based color clamping.
/// @note Intentionally made to be 16 byte aligned, fitting within 2 4-component 32 bit vector registers.
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