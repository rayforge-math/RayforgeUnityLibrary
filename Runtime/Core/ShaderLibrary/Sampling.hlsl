#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

void SampleNeighborhood(TEXTURE2D_PARAM(tex, samplerState), float2 uv, float2 texelSize, out float4 neighbors[9])
{
    static const float2 offsets[9] =
    {
        float2(-1, -1), float2(0, -1), float2(1, -1),
        float2(-1, 0), float2(0, 0), float2(1, 0),
        float2(-1, 1), float2(0, 1), float2(1, 1)
    };

    [unroll]
    for (int i = 0; i < 9; ++i)
    {
        float2 sampleUV = uv + offsets[i] * texelSize;
        sampleUV = clamp(sampleUV, float2(0,0), float2(1,1));
        neighbors[i] = SAMPLE_TEXTURE2D(tex, samplerState, sampleUV);
    }
}