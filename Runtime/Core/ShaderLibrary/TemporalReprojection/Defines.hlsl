#pragma once

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