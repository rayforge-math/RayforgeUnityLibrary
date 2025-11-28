#pragma once

// ============================================================================
// CustomUnityLibrary - Common Shader Include
// Author: Matthew
// Description: pipeline independant HLSL blur functions
// ============================================================================

// ============================================================================
// 1. Includes
// ============================================================================

#include "Packages/eu.rayforge.unitylibrary/Runtime/Core/Resources/Shaders/Common.hlsl"

// ============================================================================
// 2. Utility Functions
// ============================================================================

/// <summary>
/// Applies a 1D box blur along a given direction.
/// All samples within the radius contribute equally, making this one of the
/// simplest and fastest blur kernels.
/// </summary>
/// <param name="BlitTexture">The input texture to read from.</param>
/// <param name="samplerState">The sampler state used for texture access.</param>
/// <param name="texcoord">UV coordinate of the current pixel.</param>
/// <param name="radius">Number of samples taken to each side of the center pixel.</param>
/// <param name="direction">
/// Blur direction, e.g., (1,0) for horizontal or (0,1) for vertical.
/// </param>
/// <param name="texelSize">Size of a single texel in UV space.</param>
/// <param name="cutoff">
/// If true, samples outside UV range are discarded to avoid leaking colors.
/// </param>
/// <returns>
/// The averaged result of all valid samples in the 1D box kernel.
/// </returns>
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

/// <summary>
/// Performs a separable approximation of a 2D box blur by applying
/// one horizontal and one vertical 1D box blur pass and averaging the results.
/// This is significantly cheaper than a full 2D kernel.
/// </summary>
/// <param name="BlitTexture">Texture being blurred.</param>
/// <param name="samplerState">Sampler used for texture reads.</param>
/// <param name="texcoord">Current pixel UV.</param>
/// <param name="radius">Kernel radius for each 1D blur pass.</param>
/// <param name="scatter">
/// Scaling factor applied to sampling offsets, controlling blur spread.
/// </param>
/// <param name="texelSize">UV size of one texel.</param>
/// <param name="cutoff">If enabled, prevents sampling outside valid UV bounds.</param>
/// <returns>
/// The average of horizontal and vertical box-blur passes,
/// approximating a 2D box blur at reduced cost.
/// </returns>
float4 BoxBlurSeparableApprox(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, int radius, float scatter, float2 texelSize, bool cutoff)
{
    float4 result = (float4) 0;
    result += BoxBlur(BlitTexture, samplerState, texcoord, radius, float2(1, 0) * scatter, texelSize, cutoff);
    result += BoxBlur(BlitTexture, samplerState, texcoord, radius, float2(0, 1) * scatter, texelSize, cutoff);
    return result * 0.5;
}

/// <summary>
/// Computes a full 2D box blur by sampling in both X and Y directions.
/// All samples within the square kernel have equal weight.
/// Produces a uniform blur but is more expensive than the separable version.
/// </summary>
/// <param name="BlitTexture">Texture that will be blurred.</param>
/// <param name="samplerState">Sampler state for texture access.</param>
/// <param name="texcoord">UV of the current pixel.</param>
/// <param name="radius">Box kernel radius in both dimensions.</param>
/// <param name="scatter">
/// Controls sampling offset scaling, affecting blur size.
/// </param>
/// <param name="texelSize">UV size of a texel.</param>
/// <param name="cutoff">If true, samples outside the UV range are ignored.</param>
/// <returns>
/// The normalized sum of all box-filter samples within a square kernel.
/// </returns>
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

/// <summary>
/// Applies a 1D Gaussian blur along a specified direction using a supplied kernel.
/// Gaussian weights emphasize the center and fade smoothly outward,
/// producing a natural, soft blur.
/// </summary>
/// <param name="BlitTexture">The source texture.</param>
/// <param name="samplerState">Sampler used when reading texels.</param>
/// <param name="texcoord">UV coordinate of the pixel.</param>
/// <param name="kernel">
/// Precomputed Gaussian kernel values for offsets 0..radius.
/// </param>
/// <param name="radius">Number of Gaussian samples to each side.</param>
/// <param name="direction">Direction of blur (e.g., horizontal or vertical).</param>
/// <param name="texelSize">UV size of a texel.</param>
/// <param name="cutoff">
/// When true, samples outside the valid UV area are excluded.
/// </param>
/// <returns>
/// The Gaussian-filtered pixel value normalized by the sum of valid weights.
/// </returns>
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

/// <summary>
/// Approximates a full 2D Gaussian blur using two passes:
/// one horizontal and one vertical Gaussian convolution.
/// This is mathematically equivalent to a full 2D Gaussian blur
/// but significantly cheaper to compute.
/// </summary>
/// <param name="BlitTexture">Texture to blur.</param>
/// <param name="samplerState">Texture sampler state.</param>
/// <param name="texcoord">UV of the processed pixel.</param>
/// <param name="kernel">Gaussian kernel containing weights for 0..radius.</param>
/// <param name="radius">Blur radius.</param>
/// <param name="scatter">
/// Factor controlling offset scaling, effectively modifying blur width.
/// </param>
/// <param name="texelSize">Size of a texel in UV coordinates.</param>
/// <param name="cutoff">If enabled, prevents reading outside UV boundaries.</param>
/// <returns>
/// The average of horizontal and vertical Gaussian blur passes.
/// </returns>
float4 GaussianBlurSeparableApprox(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, StructuredBuffer<float> kernel, int radius, float scatter, float2 texelSize, bool cutoff)
{
    float4 result = (float4) 0;
    result += GaussianBlur(BlitTexture, samplerState, texcoord, kernel, radius, float2(1, 0) * scatter, texelSize, cutoff);
    result += GaussianBlur(BlitTexture, samplerState, texcoord, kernel, radius, float2(0, 1) * scatter, texelSize, cutoff);
    return result * 0.5;
}

/// <summary>
/// Computes a full 2D Gaussian blur using a separable kernel product:
/// weight(x,y) = kernel[x] * kernel[y].
/// This produces a high-quality isotropic blur but is more expensive
/// than the separable approximation.
/// </summary>
/// <param name="BlitTexture">Input texture.</param>
/// <param name="samplerState">Texture sampler used for access.</param>
/// <param name="texcoord">UV coordinate of the pixel being filtered.</param>
/// <param name="kernel">
/// 1D Gaussian kernel whose values are multiplied to form the 2D kernel.
/// </param>
/// <param name="radius">Gaussian kernel radius.</param>
/// <param name="scatter">
/// Multiplier for sample offsets (controls blur extent).
/// </param>
/// <param name="texelSize">Size of a texel in UV space.</param>
/// <param name="cutoff">
/// If true, UVs outside the [0,1] range are rejected.
/// </param>
/// <returns>
/// The normalized weighted sum of all samples in the 2D Gaussian kernel.
/// </returns>
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

/// <summary>
/// Applies a 1D tent filter blur along a specified direction.
/// The tent kernel assigns linearly decreasing weights from the center outward,
/// producing a smooth, soft blur with sharper falloff compared to Gaussian.
/// </summary>
/// <param name="BlitTexture">The input texture to sample from.</param>
/// <param name="samplerState">The sampler state used for texture reads.</param>
/// <param name="texcoord">The UV coordinate of the current pixel.</param>
/// <param name="radius">The blur radius defining the tent kernel size.</param>
/// <param name="direction">
/// The blur direction (e.g., (1,0) for horizontal or (0,1) for vertical).
/// </param>
/// <param name="texelSize">The size of a single texel in UV space.</param>
/// <param name="cutoff">
/// If true, pixels outside the normalized UV range are excluded to prevent bleeding.
/// </param>
/// <returns>
/// The tent-filtered pixel color computed using weighted samples along the given axis.
/// </returns>
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

/// <summary>
/// Applies a separable approximation of a 2D tent blur.
/// The function performs two 1D tent blurs (horizontal and vertical)
/// and averages them to approximate a full 2D tent convolution at lower cost.
/// </summary>
/// <param name="BlitTexture">The input texture.</param>
/// <param name="samplerState">The sampler to use for texture sampling.</param>
/// <param name="texcoord">The UV coordinate of the pixel being processed.</param>
/// <param name="radius">The tent blur radius for both passes.</param>
/// <param name="scatter">
/// A scaling factor controlling how far sampling offsets spread in each direction.
/// </param>
/// <param name="texelSize">The UV size of a single texel.</param>
/// <param name="cutoff">
/// If true, samples leaving the UV [0,1] range are rejected.
/// </param>
/// <returns>
/// The averaged result of horizontal and vertical tent blurs,
///
float4 TentBlurSeparableApprox(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, int radius, float scatter, float2 texelSize, bool cutoff)
{
    float4 result = (float4) 0;
    result += TentBlur(BlitTexture, samplerState, texcoord, radius, float2(1, 0) * scatter, texelSize, cutoff);
    result += TentBlur(BlitTexture, samplerState, texcoord, radius, float2(0, 1) * scatter, texelSize, cutoff);
    return result * 0.5;
}

/// <summary>
/// Performs a full 2D tent blur by sampling in a square kernel region.
/// Sample weights follow a tent distribution, decreasing linearly
/// with distance from the center pixel. Produces a smooth isotropic blur
/// with higher quality than the separable approximation but at higher cost.
/// </summary>
/// <param name="BlitTexture">The input texture.</param>
/// <param name="samplerState">The sampler used when reading texels.</param>
/// <param name="texcoord">The pixel UV position.</param>
/// <param name="radius">The blur radius defining the tent kernel extent.</param>
/// <param name="scatter">
/// Multiplier for UV offset mag
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

/// <summary>
/// Applies a Kawase blur, an efficient multi-tap downsample-style blur.
/// Kawase blur offsets samples outward in successive passes with decreasing weight,
/// producing a soft bloom-like blur at low computational cost.
/// </summary>
/// <param name="BlitTexture">The texture to blur.</param>
/// <param name="samplerState">Sampler state for texture fetches.</param>
/// <param name="texcoord">UV coordinate of the pixel.</param>
/// <param name="radius">
/// Number of iterations; higher values increase blur softness and spread.
/// </param>
/// <param name="scatter">
/// Controls how far each pass offsets its sampling positions.
/// </param>
/// <param name="texelSize">Size of one texel in UV space.</param>
/// <param name="cutoff">
/// If true, samples outside UV bounds
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

/// <summary>
/// Applies a 1D directional blur in the specified direction.
/// </summary>
/// <remarks>
/// The blur is performed along a user-defined direction vector.  
/// Depending on <paramref name="blurMode"/>, this selects between
/// Box, Gaussian, Tent, or other blur kernels.  
/// 
/// The <paramref name="scatter"/> value scales the directional offset,
/// controlling how far samples spread along the blur axis.
/// </remarks>
/// <param name="BlitTexture">Source texture to blur.</param>
/// <param name="samplerState">Sampler state for texture access.</param>
/// <param name="texcoord">UV coordinate to sample.</param>
/// <param name="kernel">Blur kernel used for Gaussian modes.</param>
/// <param name="radius">Radius of the blur kernel.</param>
/// <param name="direction">Normalized direction of the blur pass.</param>
/// <param name="scatter">Strength of sample spread along the direction.</param>
/// <param name="blurMode">Specifies which blur algorithm to apply.</param>
/// <param name="texelSize">Inverse texture resolution (pixel step size).</param>
/// <param name="cutoff">If true, stops sampling when weights become negligible.</param>
/// <returns>A blurred color sample taken along the specified direction.</returns>
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

/// <summary>
/// Performs an approximate separable blur pass.
/// </summary>
/// <remarks>
/// Executes a blur by sampling only along one axis (horizontal or vertical),
/// depending on how the function is used.  
/// 
/// This is an optimized blur technique for modes that support separability,
/// such as Gaussian, Box, or Tent blurs, enabling significantly better 
/// performance compared to a full 2D convolution.
/// </remarks>
/// <param name="BlitTexture">Source texture to blur.</param>
/// <param name="samplerState">Sampler used for sampling the texture.</param>
/// <param name="texcoord">UV coordinates for sampling.</param>
/// <param name="kernel">Kernel weights (Gaussian mode).</param>
/// <param name="radius">Blur radius determining kernel width.</param>
/// <param name="scatter">Scalar applied to sampling offsets.</param>
/// <param name="blurMode">Determines the blur algorithm to apply.</param>
/// <param name="texelSize">Pixel step size used to move along the axis.</param>
/// <param name="cutoff">If true, stops sampling early when values are small.</param>
/// <returns>
/// A blurred sample computed using a fast separable approximation.
/// </returns>
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

/// <summary>
/// Performs a full 2D convolution blur on the source texture.
/// </summary>
/// <remarks>
/// This method computes blur contributions in both X and Y directions,
/// producing more accurate results than the separable approximation,
/// at the cost of additional sampling.  
/// 
/// Useful for effects where isotropic smoothing or higher-quality
/// blurring is required.
/// </remarks>
/// <param name="BlitTexture">Texture to be blurred.</param>
/// <param name="samplerState">Sampler used for texture reads.</param>
/// <param name="texcoord">Sample position in UV space.</param>
/// <param name="kernel">Kernel weights for Gaussian blurs.</param>
/// <param name="radius">Blur radius controlling kernel width.</param>
/// <param name="scatter">Sample spread applied uniformly in 2D.</param>
/// <param name="blurMode">Determines which blur algorithm to run.</param>
/// <param name="texelSize">Size of one pixel in UV space.</param>
/// <param name="cutoff">If true, stops sampling early when weight fades.</param>
/// <returns>A fully 2D-blurred pixel color.</returns>
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

/// <summary>
/// Applies a radial blur centered around the screen origin.
/// </summary>
/// <remarks>
/// Computes a blur along a radial direction defined by the vector
/// from the screen center to the current UV coordinate.  
/// 
/// This produces effects such as motion streaks, zoom blurs, or 
/// shockwave-like distortions.  
/// 
/// The amount of blur is scaled by <paramref name="scatter"/>,
/// determining how far samples extend outward or inward.
/// </remarks>
/// <param name="BlitTexture">Input texture to blur.</param>
/// <param name="samplerState">Sampler state for texture lookups.</param>
/// <param name="texcoord">UV coordinate used to define radial direction.</param>
/// <param name="kernel">Kernel used for Gaussian blur mode.</param>
/// <param name="radius">Radius defining number of blur samples.</param>
/// <param name="scatter">Strength of radial displacement.</param>
/// <param name="blurMode">Blur algorithm selection.</param>
/// <param name="texelSize">Pixel step size for sampling.</param>
/// <param name="cutoff">Enables early termination of sampling.</param>
/// <returns>A radially blurred color sample.</returns>
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

/// <summary>
/// Applies a band-pass filter by isolating mid-frequency image details.
/// </summary>
/// <remarks>
/// This function performs two separable blur passes: 
/// a short-radius blur capturing the full spectrum 
/// and a long-radius blur isolating low-frequency signals.
/// Using subtraction a frequency band in between is obtained,
/// isolating bright spots while reducing noise.
/// 
/// Negative values are clamped to zero to avoid artifacts.
/// </remarks>
/// <param name="BlitTexture">Source texture to be filtered.</param>
/// <param name="samplerState">Sampler state used for texture lookups.</param>
/// <param name="blurMode">Blur kernel mode used by the separable blur function.</param>
/// <param name="shortKernel">Kernel for the short-radius blur pass.</param>
/// <param name="shortRadius">Radius of the short blur, capturing finer detail.</param>
/// <param name="longKernel">Kernel for the long-radius blur pass.</param>
/// <param name="longRadius">Radius of the long blur, capturing coarse detail.</param>
/// <param name="texcoord">UV coordinates for sampling.</param>
/// <param name="texelSize">Inverse texture resolution for pixel stepping.</param>
/// <returns>
/// A float3 color value representing the band-pass filtered result,
/// containing only mid-frequency features.
/// </returns>
float3 BandPass(TEXTURE2D(BlitTexture), SAMPLER(samplerState), int blurMode, StructuredBuffer<float> shortKernel, int shortRadius, StructuredBuffer<float> longKernel, int longRadius, float2 texcoord, float2 texelSize)
{
    float3 blurShort = SeparableBlurApprox(BlitTexture, samplerState, texcoord, shortKernel, shortRadius, 1, blurMode, texelSize, true).rgb;
    float3 blurLong = SeparableBlurApprox(BlitTexture, samplerState, texcoord, longKernel, longRadius, 1, blurMode, texelSize, true).rgb;
    return max((float3) 0, blurShort - blurLong);
}