#pragma once

// ============================================================================
// CustomUnityLibrary - Common Shader Include
// Author: Matthew
// Description: pipeline independant HLSL blur functions
// ============================================================================

// ============================================================================
// 1. Includes
// ============================================================================

#include "Packages/customunitylibrary/Runtime/Core/Resources/Common.hlsl"

// ============================================================================
// 2. Utility Functions
// ============================================================================

float4 BoxBlur(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, int radius, float2 direction, float2 texelSize, bool cutoff)
{
    float4 result = (float4) 0;
            //float weight = 1.0 / (2 * radius + 1);
    float count = 0;

    for (int i = -radius; i <= radius; ++i)
    {
        float2 offset = direction * float(i) * texelSize;
        float2 uv = texcoord + offset;

        if (UvInBounds(uv, cutoff))
        {
            result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv); // * weight;
            count += 1.0;
        }
    }

    return result / count;
}

float4 BoxBlurSeparableApprox(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, int radius, float scatter, float2 texelSize, bool cutoff)
{
    float4 result = (float4) 0;
    result += BoxBlur(BlitTexture, samplerState, texcoord, radius, float2(1, 0) * scatter, texelSize, cutoff);
    result += BoxBlur(BlitTexture, samplerState, texcoord, radius, float2(0, 1) * scatter, texelSize, cutoff);
    return result * 0.5;
}

float4 BoxBlur2d(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, int radius, float scatter, float2 texelSize, bool cutoff)
{
    float4 result = 0;
            //float weight = 1.0 / pow(2.0 * radius + 1.0, 2.0);
    float count = 0;

    for (int y = -radius; y <= radius; y++)
    {
        for (int x = -radius; x <= radius; x++)
        {
            float2 uv = texcoord + float2(x, y) * texelSize * scatter;
            if (UvInBounds(uv, cutoff))
            {
                result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv); // * weight;
                count += 1.0;
            }
        }
    }

    return result / count;
}

float4 GaussianBlur(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, StructuredBuffer<float> kernel, int radius, float2 direction, float2 texelSize, bool cutoff)
{
    float sum = kernel[0];
float4 result = SAMPLE_TEXTURE2D(BlitTexture, samplerState, texcoord) * sum;

    for (int i = 1; i <= radius; ++i)
    {
        float w = kernel[i];
        float2 offset = direction * float(i) * texelSize;

        float2 uv = texcoord - offset;
        if (UvInBounds(uv, cutoff))
        {
            result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv) * w;
            sum += w;
        }
        uv = texcoord + offset;
        if (UvInBounds(uv, cutoff))
        {
            result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv) * w;
            sum += w;
        }
    }
            
    return result / sum;
}

float4 GaussianBlurSeparableApprox(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, StructuredBuffer<float> kernel, int radius, float scatter, float2 texelSize, bool cutoff)
{
    float4 result = (float4) 0;
    result += GaussianBlur(BlitTexture, samplerState, texcoord, kernel, radius, float2(1, 0) * scatter, texelSize, cutoff);
    result += GaussianBlur(BlitTexture, samplerState, texcoord, kernel, radius, float2(0, 1) * scatter, texelSize, cutoff);
    return result * 0.5;
}

float4 GaussianBlur2D(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, StructuredBuffer<float> kernel, int radius, float scatter, float2 texelSize, bool cutoff)
{
    float4 result = 0;
    float sum = 0;

    for (int y = -radius; y <= radius; ++y)
    {
        for (int x = -radius; x <= radius; ++x)
        {
            float2 offset = float2(x, y) * texelSize * scatter;

            float w = kernel[abs(x)] * kernel[abs(y)];

            float2 uv = texcoord + offset;
            if (UvInBounds(uv, cutoff))
            {
                result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv) * w;
                sum += w;
            }
        }
    }

    return result / sum;
}

float4 TentBlur(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, int radius, float2 direction, float2 texelSize, bool cutoff)
{
    float sum = radius + 1;
    float4 result = SAMPLE_TEXTURE2D(BlitTexture, samplerState, texcoord) * sum;

    for (int i = 1; i <= radius; ++i)
    {
        float w = radius - i + 1;
        float2 offset = direction * float(i) * texelSize;

        float2 uv = texcoord - offset;
        if (UvInBounds(uv, cutoff))
        {
            result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv) * w;
            sum += w;
        }
        uv = texcoord + offset;
        if (UvInBounds(uv, cutoff))
        {
            result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv) * w;
            sum += w;
        }
    }

    return result / sum;
}

float4 TentBlurSeparableApprox(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, int radius, float scatter, float2 texelSize, bool cutoff)
{
    float4 result = (float4) 0;
    result += TentBlur(BlitTexture, samplerState, texcoord, radius, float2(1, 0) * scatter, texelSize, cutoff);
    result += TentBlur(BlitTexture, samplerState, texcoord, radius, float2(0, 1) * scatter, texelSize, cutoff);
    return result * 0.5;
}

float4 TentBlur2D(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, int radius, float scatter, float2 texelSize, bool cutoff)
{
    float4 result = (float4) 0;
    float sum = 0.0;

    for (int y = -radius; y <= radius; ++y)
    {
        for (int x = -radius; x <= radius; ++x)
        {
            float w = float((radius + 1) - max(abs(x), abs(y)));

            w = max(w, 0.0);

            float2 offset = float2(x, y) * texelSize * scatter;
            float2 uv = texcoord + offset;

            if (UvInBounds(uv, cutoff))
            {
                result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv) * w;
                sum += w;
            }
        }
    }

    return result / sum;
}

float4 KawaseBlur(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, int radius, float scatter, float2 texelSize, bool cutoff)
{
    float4 result = 0;
    float totalWeight = 0;

    for (int i = 0; i < radius; ++i)
    {
        float passWeight = 1.0 / (i + 1.0);
        float2 scaledTexel = texelSize * (scatter * (i + 1));

        float2 offsets[4] =
        {
            float2(1, 1),
                    float2(-1, 1),
                    float2(1, -1),
                    float2(-1, -1)
        };

                [unroll]
        for (int j = 0; j < 4; ++j)
        {
            float2 uv = texcoord + offsets[j] * scaledTexel;
            if (UvInBounds(uv, cutoff))
            {
                float4 s = SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv);
                result += s * passWeight;
                totalWeight += passWeight;
            }
        }
    }

    return result / max(totalWeight, 1e-5);
}

float4 DirectionalBlur(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, StructuredBuffer<float> kernel, int radius, float2 direction, float scatter, int blurMode, float2 texelSize, bool cutoff)
{
    float4 color = (float4) 0;

    switch (blurMode)
    {
        case 0:
            color = SAMPLE_TEXTURE2D(BlitTexture, samplerState, texcoord);
            break;
        case 1:
            color = BoxBlur(BlitTexture, samplerState, texcoord, radius, direction * scatter, texelSize, cutoff);
            break;
        case 2:
            color = GaussianBlur(BlitTexture, samplerState, texcoord, kernel, radius, direction * scatter, texelSize, cutoff);
            break;
        case 3:
            color = TentBlur(BlitTexture, samplerState, texcoord, radius, direction * scatter, texelSize, cutoff);
            break;
        case 4:
                    //color = KawaseBlur(BlitTexture, samplerState, texcoord, radius, scatter, texelSize, cutoff);
            break;
    }

    return color;
}

float4 SeparableBlurApprox(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, StructuredBuffer<float> kernel, int radius, float scatter, int blurMode, float2 texelSize, bool cutoff)
{
    float4 color = (float4) 0;

    switch (blurMode)
    {
        case 0:
            color = SAMPLE_TEXTURE2D(BlitTexture, samplerState, texcoord);
            break;
        case 1:
            color = BoxBlurSeparableApprox(BlitTexture, samplerState, texcoord, radius, scatter, texelSize, cutoff);
            break;
        case 2:
            color = GaussianBlurSeparableApprox(BlitTexture, samplerState, texcoord, kernel, radius, scatter, texelSize, cutoff);
            break;
        case 3:
            color = TentBlurSeparableApprox(BlitTexture, samplerState, texcoord, radius, scatter, texelSize, cutoff);
            break;
        case 4:
            color = KawaseBlur(BlitTexture, samplerState, texcoord, radius, scatter, texelSize, cutoff);
            break;
    }

    return color;
}

float4 Blur2D(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, StructuredBuffer<float> kernel, int radius, float scatter, int blurMode, float2 texelSize, bool cutoff)
{
    float4 color = (float4) 0;

    switch (blurMode)
    {
        case 0:
            color = SAMPLE_TEXTURE2D(BlitTexture, samplerState, texcoord);
            break;
        case 1:
            color = BoxBlur2d(BlitTexture, samplerState, texcoord, radius, scatter, texelSize, cutoff);
            break;
        case 2:
            color = GaussianBlur2D(BlitTexture, samplerState, texcoord, kernel, radius, scatter, texelSize, cutoff);
            break;
        case 3:
            color = TentBlur2D(BlitTexture, samplerState, texcoord, radius, scatter, texelSize, cutoff);
            break;
        case 4:
            color = KawaseBlur(BlitTexture, samplerState, texcoord, radius, scatter, texelSize, cutoff);
            break;
    }

    return color;
}

float4 RadialBlur(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, StructuredBuffer<float> kernel, int radius, float scatter, int blurMode, float2 texelSize, bool cutoff)
{
    float2 direction = texcoord * 2.0 - 1.0;

    float4 color = (float4) 0;

    switch (blurMode)
    {
        case 0:
            color = SAMPLE_TEXTURE2D(BlitTexture, samplerState, texcoord);
            break;
        case 1:
            color = BoxBlur(BlitTexture, samplerState, texcoord, radius, direction * scatter, texelSize, cutoff);
            break;
        case 2:
            color = GaussianBlur(BlitTexture, samplerState, texcoord, kernel, radius, direction * scatter, texelSize, cutoff);
            break;
        case 3:
            color = TentBlur(BlitTexture, samplerState, texcoord, radius, direction * scatter, texelSize, cutoff);
            break;
        case 4:
                    //color = KawaseBlur(BlitTexture, samplerState, texcoord, radius, scatter, texelSize, cutoff);
            break;
    }

    return color;
}