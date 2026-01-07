#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

TEXTURE2D(_Rayforge_BlueNoise);
SAMPLER(sampler_Rayforge_BlueNoise);
float4 _Rayforge_BlueNoise_TexelSize;