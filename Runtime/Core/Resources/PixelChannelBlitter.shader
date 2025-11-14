Shader "Rayforge/PixelChannelBlitter"
{
    Properties
    {
        _BlitSource("Source Texture", 2D) = "white" {}
        _BlitParams("Blit Params", Vector) = (1,1,1,1)
        _R("R Channel Mapping", Int) = 0
        _G("G Channel Mapping", Int) = 1
        _B("B Channel Mapping", Int) = 2
        _A("A Channel Mapping", Int) = 3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off ZWrite Off

        HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex Vert
            #pragma fragment ChannelBlitterFrag
            
            static const uint R = 0;
            static const uint G = 1;
            static const uint B = 2;
            static const uint A = 3;
            static const uint None = 4;

            TEXTURE2D(_BlitSource);
            float4 _BlitSource_TexelSize;
            
            cbuffer ChannelBlitterParams : register(b0)
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

            void FullscreenTriangle(uint id, inout Varyings o)
            {
                o.texcoord = float2((id << 1) & 2, id & 2);
                o.positionCS = float4(o.texcoord * 2 - 1, 0, 1);
            }

            Varyings Vert(uint id : SV_VertexID)
            {
                Varyings output = (Varyings)0;
                FullscreenTriangle(id, output);

                float2 offset = _BlitParams.xy * _BlitSource_TexelSize.xy;
                float2 scale = _BlitParams.zw * _BlitSource_TexelSize.xy;
                
                output.texcoord *= scale;
                output.texcoord += offset;

                return output;
            }

            float4 ChannelBlitterFrag(Varyings input) : SV_Target
            {
                float4 sample = SAMPLE_TEXTURE2D(_BlitSource, sampler_LinearClamp, input.texcoord);
                
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
