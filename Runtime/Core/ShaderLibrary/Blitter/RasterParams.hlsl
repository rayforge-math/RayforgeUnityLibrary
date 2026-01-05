#pragma once

TEXTURE2D_X(_BlitTexture);
CBUFFER_START(UnityPerMaterial)
float4 _BlitTexture_TexelSize;
CBUFFER_END