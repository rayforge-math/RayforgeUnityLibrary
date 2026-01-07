#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

/// @brief Parameters controlling which channels to blit and the source region.
/// @param _R Index of source channel to copy to red output (or None).
/// @param _G Index of source channel to copy to green output (or None).
/// @param _B Index of source channel to copy to blue output (or None).
/// @param _A Index of source channel to copy to alpha output (or None).
/// @param _BlitParams.xy Pixel offset in source texture (in texels).
/// @param _BlitParams.zw Size of the blit region (width, height in texels).
CBUFFER_START(_ChannelBlitterParams)
uint _R;
uint _G;
uint _B;
uint _A;
float4 _BlitParams;
CBUFFER_END