#pragma once

// ============================================================================
// MyLibrary - Common Shader Include
// Author: Matthew
// Description: pipeline independant HLSL utilities for Unity
// ============================================================================

// ============================================================================
// 1. Utility Functions
// ============================================================================

// ============================================================================
// FullscreenTriangle Vertex Helper
// Procedurally generates the vertices and UVs for a single fullscreen triangle
// to cover the entire render target. Works efficiently with
// DrawProcedural or full-screen passes without a vertex buffer.
//
// Parameters:
//   id         - Vertex index (0, 1, 2)
// Outputs:
//   positionCS - Clip-space position (SV_POSITION) in range -1..3
//   texcoord   - UV coordinates in the range 0..2
//
// (0,0)                     (1,0)                    (2,0)         <- uvs
// (-1,-1)                   (1,-1)                   (3,-1)        <- vert
// _____________________________________________________
// |                           |                    /
// |                           |                /
// |             VS            |            /
// |                           |        /
// |                           |    /
// |___________________________ /
// |                        /
// |                    /
// |                /
// |            /
// |        /
// |    /
// |/
// ============================================================================
void FullscreenTriangle(uint id, out float4 positionCS, out float2 texcoord)
{
    texcoord = float2((id << 1) & 2, id & 2);
    positionCS = float4(texcoord * 2 - 1, 0, 1);
}