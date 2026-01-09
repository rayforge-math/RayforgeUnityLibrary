#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#if !defined(TEXTURE2D_X)
    #define TEXTURE2D_X(name)                   TEXTURE2D(name)
    #define SAMPLE_TEXTURE2D_X(tex, samp, uv)   SAMPLE_TEXTURE2D(tex, samp, uv)
    #define LOAD_TEXTURE2D_X(tex, coord)        LOAD_TEXTURE2D(tex, coord)
#endif