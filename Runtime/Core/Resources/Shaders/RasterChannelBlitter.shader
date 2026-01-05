Shader "Rayforge/RasterChannelBlitter"
{
    Properties
    {
        _BlitTexture("Source Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off ZWrite Off

        HLSLPROGRAM
            #include "Packages/eu.rayforge.unitylibrary/Runtime/Core/ShaderLibrary/Blitter/RasterBlit.hlsl"

            #pragma vertex Vert
            #pragma fragment ChannelBlitterFrag

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            /// @brief Vertex shader generating a fullscreen triangle with adjusted UVs for blitting.
            /// @param id Vertex ID provided by the GPU.
            /// @return Varyings struct containing clip-space position and adjusted UV coordinates.
            Varyings Vert(uint id : SV_VertexID)
            {
                Varyings output = (Varyings)0;
                SetupBlitPipeline(id, output.positionCS, output.texcoord);
                return output;
            }

            /// @brief Fragment shader that copies selected channels from the source texture.
            /// @param input Varyings struct with UVs and clip-space position.
            /// @return The output color with selected channels copied from the source.
            float4 ChannelBlitterFrag(Varyings input) : SV_Target
            {
                return SampleBlitTexture(input.texcoord);
            }
        ENDHLSL
        }
    }
}
