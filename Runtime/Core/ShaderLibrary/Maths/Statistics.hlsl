#pragma once

/// @brief Computes the per-channel mean and standard deviation from a 3x3 neighborhood of samples.
/// @param neighborhood A fixed array of 9 float3 color samples representing the 3x3 neighborhood around the pixel.
/// @param mean Output: The per-channel arithmetic mean of the neighborhood.
/// @param stdDev Output: The per-channel standard deviation, describing how much variation exists in the neighborhood.
void ComputeMeanAndStdDev9(in float4 neighborhood[9], out float3 mean, out float3 stdDev)
{
    mean = float3(0, 0, 0);

    [unroll]
    for (int i = 0; i < 9; ++i)
    {
        mean += neighborhood[i].rgb;
    }
    mean /= 9.0;

    float3 var = float3(0, 0, 0);
    [unroll]
    for (int j = 0; j < 9; ++j)
    {
        float3 d = neighborhood[j].rgb - mean;
        var += d * d;
    }
    var /= 9.0;

    stdDev = sqrt(var);
}

/// @brief Computes the per-channel mean and standard deviation from a 3x3 neighborhood of samples, excluding the centre.
/// @param neighborhood A fixed array of 9 float3 color samples representing the 3x3 neighborhood around the pixel.
/// @param mean Output: The per-channel arithmetic mean of the neighborhood.
/// @param stdDev Output: The per-channel standard deviation, describing how much variation exists in the neighborhood.
void ComputeMeanAndStdDev8(in float4 neighborhood[9], out float3 mean, out float3 stdDev)
{
    mean = float3(0, 0, 0);

    [unroll]
    for (int i = 0; i < 9; ++i)
    {
        if (i == 4)
            continue;
        mean += neighborhood[i].rgb;
    }
    mean /= 8.0;

    float3 var = float3(0, 0, 0);
    [unroll]
    for (int j = 0; j < 9; ++j)
    {
        if (j == 4)
            continue;
        float3 d = neighborhood[j].rgb - mean;
        var += d * d;
    }
    var /= 8.0;

    stdDev = sqrt(var);
}