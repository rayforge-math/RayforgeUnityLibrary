#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/eu.rayforge.unitylibrary/Runtime/Core/ShaderLibrary/Texture.hlsl"

TEXTURE2D_X(_BlitTexture);
CBUFFER_START(UnityPerMaterial)
float4 _BlitTexture_TexelSize;
CBUFFER_END