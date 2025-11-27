Shader "Rayforge/RasterChannelBlitter"
{
    Properties
    {
        _BlitTexture("Source Texture", 2D) = "white" {}
        _BlitSource_TexelSize ("Source Texel Size", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off ZWrite Off

        HLSLPROGRAM
            #include "Packages/eu.rayforge.customunitylibrary/Runtime/Core/Resources/Shaders/Common.hlsl"

            #pragma vertex Vert
            #pragma fragment ChannelBlitterFrag
            
            static const uint R = 0;
            static const uint G = 1;
            static const uint B = 2;
            static const uint A = 3;
            static const uint None = 4;

            SamplerState sampler_LinearClamp
            {
                Filter = MIN_MAG_MIP_LINEAR;
                AddressU = Clamp;
                AddressV = Clamp;
            };

            TEXTURE2D(_BlitTexture);
            cbuffer UnityPerMaterial : register(b0)
            {
                float4 _BlitTexture_TexelSize : packoffset(c0.x);
            }

            cbuffer _ChannelBlitterParams : register(b1)
            {
                uint _R : packoffset(c0.x);
                uint _G : packoffset(c0.y);
                uint _B : packoffset(c0.z);
                uint _A : packoffset(c0.w);
                float4 _BlitParams : packoffset(c1.x);
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            Varyings Vert(uint id : SV_VertexID)
            {
                Varyings output = (Varyings)0;
                FullscreenTriangle(id, output.positionCS, output.texcoord);

                float2 offset = _BlitParams.xy * _BlitTexture_TexelSize.xy;
                float2 scale = _BlitParams.zw * _BlitTexture_TexelSize.xy;
                
                output.texcoord *= scale;
                output.texcoord += offset;

                return output;
            }

            float4 ChannelBlitterFrag(Varyings input) : SV_Target
            {
                float4 sample = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
                
                float4 dest = (float4) 0;
                if (_R < None) dest.r = sample[_R];
                if (_G < None) dest.g = sample[_G];
                if (_B < None) dest.b = sample[_B];
                if (_A < None) dest.a = sample[_A];

                return dest;
            }
        ENDHLSL
        }
    }
}
