#pragma once

#include "Packages/eu.rayforge.unitylibrary/Runtime/Core/ShaderLibrary/Sampling.hlsl"
#include "Packages/eu.rayforge.unitylibrary/Runtime/Core/ShaderLibrary/Rendering/FullscreenTriangle.hlsl"

void SetupBlitPipeline(uint id, out float4 positionCS, out float2 texcoord)
{
    FullscreenTriangle(id, positionCS, texcoord);

    float2 offset = _BlitParams.xy * _BlitTexture_TexelSize.xy;
    float2 scale = _BlitParams.zw * _BlitTexture_TexelSize.xy;
                
    texcoord *= scale;
    texcoord += offset;
}

float4 SampleBlitTexture(float2 texcoord)
{
    float4 sample = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, texcoord);
                
    float4 dest = (float4) 0;
    if (_R != BLIT_CHANNEL_NONE)
        dest.r = sample[_R];
    if (_G != BLIT_CHANNEL_NONE)
        dest.g = sample[_G];
    if (_B != BLIT_CHANNEL_NONE)
        dest.b = sample[_B];
    if (_A != BLIT_CHANNEL_NONE)
        dest.a = sample[_A];
    
    return sample;
}