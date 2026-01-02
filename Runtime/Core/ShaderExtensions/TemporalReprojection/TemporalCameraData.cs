using UnityEngine;

using Rayforge.Utility.Synchronization;

namespace Rayforge.ShaderExtensions.TemporalReprojection
{
    /// <summary>
    /// Provides per-frame temporal camera data for use in reprojection-based effects
    /// such as TAA, motion reprojection, or temporal denoising.
    ///
    /// This class manages both the previous frame's View-Projection matrix and the
    /// current frame's inverse View-Projection matrix. Both are exposed as global
    /// shader properties. The update/upload runs only once per frame, even if called
    /// multiple times.
    /// </summary>
    public static class TemporalCameraData
    {
        /// <summary>
        /// Cached previous frame's View-Projection matrix.
        /// Used by shaders to reproject current pixel positions into history buffers.
        /// </summary>
        private static Matrix4x4 s_PrevViewProjMatrix = Matrix4x4.identity;

        /// <summary>
        /// Cached inverse of the current frame's View-Projection matrix.
        /// Used by shaders to reconstruct world-space positions.
        /// </summary>
        private static Matrix4x4 s_InvViewProjMatrix = Matrix4x4.identity;

        /// <summary>
        /// Per-frame execution guard.
        /// Ensures matrix updates and uploads occur only once per frame.
        /// </summary>
        private static FrameOnce s_FrameOnce;

        /// <summary>
        /// Shader property ID for the previous View-Projection matrix.
        /// </summary>
        private static readonly int k_PrevViewProjectionMatrixId =
            Shader.PropertyToID("_Rayforge_Matrix_Prev_VP");

        /// <summary>
        /// Shader property ID for the inverse View-Projection matrix.
        /// </summary>
        private static readonly int k_InvViewProjectionMatrixId =
            Shader.PropertyToID("_Rayforge_Matrix_Inv_VP");

        /// <summary>
        /// Updates and uploads temporal camera matrices once per frame.
        ///
        /// Safe to call multiple times per frame and from multiple systems.
        /// </summary>
        public static void UpdateViewProjectionMatrices()
        {
            if (!s_FrameOnce.TryBegin())
                return;

            Matrix4x4 currentViewProj = Matrix4x4.identity;

            var camera = Camera.main;
            if (camera != null)
            {
                currentViewProj = camera.projectionMatrix * camera.worldToCameraMatrix;
            }

            // Compute inverse VP for world-space reconstruction
            s_InvViewProjMatrix = Matrix4x4.Inverse(currentViewProj);

            // Upload global shader parameters
            Shader.SetGlobalMatrix(k_InvViewProjectionMatrixId, s_InvViewProjMatrix);
            Shader.SetGlobalMatrix(k_PrevViewProjectionMatrixId, s_PrevViewProjMatrix);

            // Cache current VP for next frame
            s_PrevViewProjMatrix = currentViewProj;
        }
    }
}