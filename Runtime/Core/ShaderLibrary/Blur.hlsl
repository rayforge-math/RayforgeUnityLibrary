#pragma once

// ============================================================================
// CustomUnityLibrary - Common Shader Include
// Author: Matthew
// Description: pipeline independant HLSL blur functions
// ============================================================================

// ============================================================================
// 1. Includes
// ============================================================================

#include "Packages/eu.rayforge.unitylibrary/Runtime/Core/ShaderLibrary/Common.hlsl"

// ============================================================================
// 2. Utility Functions
// ============================================================================

/// @brief Applies a 1D box blur along a given direction.
///        All samples within the radius contribute equally, making this a simple and fast blur.
/// @param BlitTexture The input texture to read from
/// @param samplerState The sampler state used for texture access
/// @param texcoord UV coordinate of the current pixel
/// @param radius Number of samples taken to each side of the center pixel
/// @param direction Blur direction, e.g., (1,0) for horizontal or (0,1) for vertical
/// @param texelSize Size of a single texel in UV space
/// @param cutoff If true, samples outside UV range are discarded to avoid leaking colors
/// @return The averaged result of all valid samples in the 1D box kernel
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

/// @brief Performs a separable approximation of a 2D box blur by applying
///        one horizontal and one vertical 1D blur pass, averaging results.
///        This is cheaper than a full 2D kernel.
/// @param BlitTexture Texture being blurred
/// @param samplerState Sampler used for texture reads
/// @param texcoord Current pixel UV
/// @param radius Kernel radius for each 1D blur pass
/// @param scatter Scaling factor applied to sampling offsets, controlling blur spread
/// @param texelSize UV size of one texel
/// @param cutoff If enabled, prevents sampling outside valid UV bounds
/// @return The average of horizontal and vertical box-blur passes, approximating a 2D box blur
float4 BoxBlurSeparableApprox(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, int radius, float scatter, float2 texelSize, bool cutoff)
{
    float4 result = (float4) 0;
    result += BoxBlur(BlitTexture, samplerState, texcoord, radius, float2(1, 0) * scatter, texelSize, cutoff);
    result += BoxBlur(BlitTexture, samplerState, texcoord, radius, float2(0, 1) * scatter, texelSize, cutoff);
    return result * 0.5;
}

/// @brief Computes a full 2D box blur by sampling in both X and Y directions.
///        All samples within the square kernel have equal weight.
///        Produces a uniform blur but is more expensive than the separable version.
/// @param BlitTexture Texture that will be blurred
/// @param samplerState Sampler state for texture access
/// @param texcoord UV of the current pixel
/// @param radius Box kernel radius in both dimensions
/// @param scatter Scaling factor for sampling offsets (affects blur size)
/// @param texelSize UV size of a texel
/// @param cutoff If true, samples outside UV range are ignored
/// @return Normalized sum of all box-filter samples within the square kernel
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

/// @brief Applies a 1D Gaussian blur along a specified direction using a supplied kernel.
///        Gaussian weights emphasize the center and fade smoothly outward.
/// @param BlitTexture Source texture
/// @param samplerState Sampler used when reading texels
/// @param texcoord UV coordinate of the pixel
/// @param kernel Precomputed Gaussian kernel values for offsets 0..radius
/// @param radius Number of Gaussian samples to each side
/// @param direction Direction of blur (e.g., horizontal or vertical)
/// @param texelSize UV size of a texel
/// @param cutoff If true, samples outside valid UV area are excluded
/// @return Gaussian-filtered pixel value normalized by sum of valid weights
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

/// @brief Approximates a full 2D Gaussian blur using two 1D passes.
/// @param BlitTexture Texture to blur
/// @param samplerState Sampler for texture access
/// @param texcoord UV of the processed pixel
/// @param kernel Gaussian kernel containing weights for 0..radius
/// @param radius Blur radius
/// @param scatter Scaling factor for sample offsets
/// @param texelSize Size of a texel in UV coordinates
/// @param cutoff Prevents reading outside UV boundaries if true
/// @return Average of horizontal and vertical Gaussian blur passes
float4 GaussianBlurSeparableApprox(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, StructuredBuffer<float> kernel, int radius, float scatter, float2 texelSize, bool cutoff)
{
    float4 result = (float4) 0;
    result += GaussianBlur(BlitTexture, samplerState, texcoord, kernel, radius, float2(1, 0) * scatter, texelSize, cutoff);
    result += GaussianBlur(BlitTexture, samplerState, texcoord, kernel, radius, float2(0, 1) * scatter, texelSize, cutoff);
    return result * 0.5;
}

/// @brief Computes a full 2D Gaussian blur using a separable kernel product: weight(x,y) = kernel[x] * kernel[y].
///        Produces a high-quality isotropic blur.
/// @param BlitTexture Input texture
/// @param samplerState Sampler for texture reads
/// @param texcoord UV of the pixel
/// @param kernel 1D Gaussian kernel
/// @param radius Kernel radius
/// @param scatter Multiplier for sample offsets
/// @param texelSize UV size of a texel
/// @param cutoff Reject UVs outside [0,1] range if true
/// @return Normalized weighted sum of all samples in the 2D Gaussian kernel
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

/// @brief Applies a 1D tent filter blur along a specified direction.
///        Weights decrease linearly from the center outward.
/// @param BlitTexture Input texture to sample from
/// @param samplerState Sampler state for texture reads
/// @param texcoord UV of the current pixel
/// @param radius Blur radius defining kernel size
/// @param direction Blur direction (e.g., (1,0) horizontal, (0,1) vertical)
/// @param texelSize Size of one texel in UV space
/// @param cutoff If true, excludes pixels outside [0,1] UV range
/// @return Tent-filtered pixel color along the specified axis
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

/// @brief Applies a separable approximation of a 2D tent blur.
///        Performs horizontal and vertical 1D tent blurs and averages them.
/// @param BlitTexture Input texture
/// @param samplerState Sampler for texture reads
/// @param texcoord UV of the current pixel
/// @param radius Tent blur radius
/// @param scatter Scaling factor for sampling offsets
/// @param texelSize Size of one texel in UV space
/// @param cutoff If true, excludes samples outside [0,1] UV
/// @return Average of horizontal and vertical tent blur passes
float4 TentBlurSeparableApprox(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, int radius, float scatter, float2 texelSize, bool cutoff)
{
    float4 result = (float4) 0;
    result += TentBlur(BlitTexture, samplerState, texcoord, radius, float2(1, 0) * scatter, texelSize, cutoff);
    result += TentBlur(BlitTexture, samplerState, texcoord, radius, float2(0, 1) * scatter, texelSize, cutoff);
    return result * 0.5;
}

/// @brief Performs a full 2D tent blur using a square kernel.
///        Weights decrease linearly with distance from the center pixel.
/// @param BlitTexture Input texture
/// @param samplerState Sampler for reading texels
/// @param texcoord UV of the current pixel
/// @param radius Blur radius defining kernel extent
/// @param scatter Multiplier for UV offset magnitude
/// @param texelSize Size of one texel in UV space
/// @param cutoff If true, discards samples outside [0,1] UV
/// @return Normalized tent-filtered pixel color using a 2D kernel
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

/// @brief Applies a Kawase blur, an efficient multi-tap downsample-style blur.
///        Offsets samples outward in successive passes with decreasing weight,
///        producing a soft bloom-like effect at low cost.
/// @param BlitTexture Texture to blur
/// @param samplerState Sampler state for texture fetches
/// @param texcoord UV coordinate of the pixel
/// @param radius Number of passes; higher values increase blur spread
/// @param scatter Controls offset scaling for each pass
/// @param texelSize Size of one texel in UV space
/// @param cutoff If true, ignores samples outside [0,1] UV
/// @return Kawase-blurred pixel color
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

/// @brief Applies a 1D directional blur in the given direction using the selected blur mode.
/// @param BlitTexture Source texture
/// @param samplerState Sampler state for texture access
/// @param texcoord UV coordinate to sample
/// @param kernel Kernel used for Gaussian blur (ignored for other modes)
/// @param radius Blur radius / number of samples
/// @param direction Normalized blur direction
/// @param scatter Scales the sampling offsets along the direction
/// @param blurMode 0=None, 1=Box, 2=Gaussian, 3=Tent, 4=Kawase
/// @param texelSize Size of one pixel in UV space
/// @param cutoff If true, ignores samples outside [0,1] UV
/// @return Blurred pixel along the specified direction
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

/// @brief Performs a separable 1D blur approximation along horizontal or vertical axis.
/// @param BlitTexture Source texture
/// @param samplerState Sampler used for texture access
/// @param texcoord UV to sample
/// @param kernel Gaussian kernel weights
/// @param radius Blur radius
/// @param scatter Offset multiplier
/// @param blurMode Blur algorithm selector
/// @param texelSize UV size of a pixel
/// @param cutoff Early stop flag for out-of-range sampling
/// @return Approximate separable blurred color
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

/// @brief Performs a full 2D convolution blur.
/// @param BlitTexture Source texture
/// @param samplerState Sampler for texture reads
/// @param texcoord UV position to sample
/// @param kernel Kernel for Gaussian blur
/// @param radius Blur radius
/// @param scatter Offset multiplier
/// @param blurMode Blur type
/// @param texelSize Pixel step in UV space
/// @param cutoff Early exit flag
/// @return Fully 2D blurred color
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

/// @brief Applies a radial blur centered on the screen origin.
/// @param BlitTexture Input texture
/// @param samplerState Sampler state
/// @param texcoord UV of the pixel
/// @param kernel Gaussian kernel weights
/// @param radius Number of samples
/// @param scatter Strength of radial blur
/// @param blurMode Blur type
/// @param texelSize Pixel step size in UV
/// @param cutoff Early stop flag
/// @return Radially blurred color
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

/// @brief Applies a band-pass filter isolating mid-frequency image details.
/// @remarks Performs two separable blur passes: 
/// a short-radius blur capturing fine details and a long-radius blur capturing low frequencies.
/// The band-pass is obtained by subtracting the long blur from the short blur, clamping negatives to zero.
/// @param BlitTexture Source texture
/// @param samplerState Sampler state for texture access
/// @param blurMode Blur kernel mode (used by SeparableBlurApprox)
/// @param shortKernel Kernel for short-radius blur
/// @param shortRadius Radius of the short blur
/// @param longKernel Kernel for long-radius blur
/// @param longRadius Radius of the long blur
/// @param texcoord UV coordinates to sample
/// @param texelSize Pixel size in UV space
/// @return float3 containing mid-frequency (band-pass) filtered result
float3 BandPass(TEXTURE2D(BlitTexture), SAMPLER(samplerState), int blurMode, StructuredBuffer<float> shortKernel, int shortRadius, StructuredBuffer<float> longKernel, int longRadius, float2 texcoord, float2 texelSize)
{
    float3 blurShort = SeparableBlurApprox(BlitTexture, samplerState, texcoord, shortKernel, shortRadius, 1, blurMode, texelSize, true).rgb;
    float3 blurLong = SeparableBlurApprox(BlitTexture, samplerState, texcoord, longKernel, longRadius, 1, blurMode, texelSize, true).rgb;
    return max((float3) 0, blurShort - blurLong);
}