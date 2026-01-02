#pragma once

// ============================================================================
// 1. Utility Functions
// ============================================================================

// ============================================================================
// Sketch:
// (0,0)                     (1,0)                    (2,0)         <- uvs
// (-1,-1)                   (1,-1)                   (3,-1)        <- vert (CS)
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
/// @brief FullscreenTriangle Vertex Helper.
/// Procedurally generates the vertices and UVs for a single fullscreen triangle
/// to cover the entire render target. Works efficiently with
/// DrawProcedural or full-screen passes without a vertex buffer.
/// @param id Vertex index (0, 1, 2)
/// @param positionCS Output: Clip-space position (SV_POSITION) in range -1..3
/// @param texcoord Output: UV coordinates in the range 0..2
void FullscreenTriangle(uint id, out float4 positionCS, out float2 texcoord)
{
    texcoord = float2((id << 1) & 2, id & 2);
    positionCS = float4(texcoord * 2 - 1, 0, 1);
}