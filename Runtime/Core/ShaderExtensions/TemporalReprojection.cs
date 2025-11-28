using UnityEngine;

namespace Rayforge.ShaderExtensions.TemporalReprojection
{
    /// <summary>
    /// Defines the color clamping modes used in temporal anti-aliasing (TAA) shaders.
    /// These modes control how the current frame color is clamped based on history neighborhoods
    /// to reduce ghosting and temporal artifacts.
    /// </summary>
    public enum TemporalColorClampMode : int
    {
        /// <summary>
        /// No clamping is performed; the current color is blended directly with the history.
        /// </summary>
        None = 0,

        /// <summary>
        /// Performs a simple min/max clamp of the current color against the 3×3 neighborhood
        /// of the current frame or reprojected history.
        /// Limits extreme deviations without considering statistical variance.
        /// </summary>
        ColorClamp = 1,

        /// <summary>
        /// Performs variance-based clamping using the mean and standard deviation
        /// of the 3×3 neighborhood of the current frame.
        /// Simple statistical clamp: historyColor ∈ [mean - stdDev*scale, mean + stdDev*scale].
        /// </summary>
        VarianceClamp = 2,

        /// <summary>
        /// Performs luma-oriented clip-box clamping along the principal luma direction,
        /// roughly following Unreal Engine's approach.
        /// See: https://de45xmedrsdbp.cloudfront.net/Resources/files/TemporalAA_small-59732822.pdf#page=34
        /// </summary>
        ClipBoxClamp = 3
    }

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
        /// Used by shaders to reconstruct world-space positions from NDC/clip-space coordinates.
        /// </summary>
        private static Matrix4x4 s_InvViewProjMatrix = Matrix4x4.identity;

        /// <summary>
        /// Internal frame counter ensuring that updates occur only once per frame.
        /// </summary>
        private static int s_FrameCount = -1;

        /// <summary>
        /// Shader property ID for setting the previous View-Projection matrix globally.
        /// </summary>
        private static readonly int k_PrevViewProjectionMatrixId = Shader.PropertyToID("_Rayforge_Matrix_Prev_VP");

        /// <summary>
        /// Shader property ID for setting the inverse View-Projection matrix globally.
        /// </summary>
        private static readonly int k_InvViewProjectionMatrixId = Shader.PropertyToID("_Rayforge_Matrix_Inv_VP");

        /// <summary>
        /// Updates both the previous View-Projection (VP) matrix and the current inverse VP matrix once per frame.
        ///
        /// Sets the cached previous VP matrix into a global shader variable for reprojection usage,
        /// calculates and uploads the inverse of the current VP matrix for world-space reconstruction,
        /// and refreshes the cached previous VP matrix with the current frame's matrix.
        ///
        /// Call this at the start of your rendering frame or before dispatching any
        /// reprojection-reliant shader passes.
        /// </summary>
        public static void UpdateViewProjectionMatrices()
        {
            int currentFrame = Time.frameCount;

            if (s_FrameCount == currentFrame)
                return;
            s_FrameCount = currentFrame;

            Matrix4x4 currentViewProj = Matrix4x4.identity;

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                currentViewProj = mainCamera.projectionMatrix * mainCamera.worldToCameraMatrix;
            }

            // Compute inverse of current VP matrix for reconstructing world positions in shaders
            s_InvViewProjMatrix = Matrix4x4.Inverse(currentViewProj);

            // Upload both matrices as global shader variables
            Shader.SetGlobalMatrix(k_InvViewProjectionMatrixId, s_InvViewProjMatrix);
            Shader.SetGlobalMatrix(k_PrevViewProjectionMatrixId, s_PrevViewProjMatrix);

            // Update cached previous VP for next frame
            s_PrevViewProjMatrix = currentViewProj;
        }
    }
}