using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

using static Rayforge.Utility.RuntimeCheck.Asserts;

namespace Rayforge.RenderGraphExtensions.Rendering
{
    /// <summary>
    /// Extension methods for <see cref="RTHandle"/> to simplify integration with RenderGraph.
    /// </summary>
    public static class RTHandleExtensions
    {
        /// <summary>
        /// Imports a persistent <see cref="RTHandle"/> into a <see cref="RenderGraph"/> as a <see cref="TextureHandle"/>.
        /// This allows using a pre-existing RTHandle (e.g., ping-pong history or reprojection target) within a RenderGraph pass.
        /// </summary>
        /// <param name="handle">The <see cref="RTHandle"/> to import.</param>
        /// <param name="renderGraph">The <see cref="RenderGraph"/> instance where the texture will be used.</param>
        /// <returns>A <see cref="TextureHandle"/> representing the imported <see cref="RTHandle"/> inside the render graph.</returns>
        public static TextureHandle ToRenderGraphHandle(this RTHandle handle, RenderGraph renderGraph)
            => renderGraph.ImportTexture(handle);
    }

    /// <summary>
    /// Manages a pair of persistent render targets for temporal reprojection using a ping-pong scheme.
    /// One handle is used as the current target (write), the other as the previous frame's history (read).
    /// </summary>
    public struct ReprojectionHandles<Tdesc>
            where Tdesc : struct
    {
        /// <summary>
        /// Function signature for creating a texture handle for history buffers.
        /// </summary>
        /// <returns>
        /// <c>true</c> if a handle has been allocated;
        /// <c>false</c> if no allocation was necessary.
        /// </returns>
        public delegate bool TextureReAllocFunction(ref RTHandle handle, Tdesc descriptor, string name);

        private RTHandle[] m_Handles;
        private string[] m_HandleNames;
        private TextureReAllocFunction m_ReAllocFunc;
        private int m_CurrentTarget;

        /// <summary>
        /// Gets the handle containing the previous frame's history (read target).
        /// </summary>
        public RTHandle History => m_Handles[NextIndex(m_CurrentTarget)];

        /// <summary>
        /// Gets the handle used as the current frame's target (write target).
        /// </summary>
        public RTHandle Target => m_Handles[m_CurrentTarget];

        /// <summary>
        /// Initializes a new instance of the <see cref="ReprojectionHandles{Tdesc}"/> struct.
        /// Sets up the delegate for handle creation and precomputes the names for the ping-pong handles.
        /// </summary>
        /// <param name="createFunc">Delegate used to create or reallocate handles.</param>
        /// <param name="handleName">Optional base name used for the ping-pong handles for debugging/profiling.</param>
        public ReprojectionHandles(TextureReAllocFunction reAllocFunc, string handleName = "")
        {
            ValidateDelegate(reAllocFunc);
            m_ReAllocFunc = reAllocFunc;
            m_Handles = Array.Empty<RTHandle>();
            m_HandleNames = new string[2];
            for (int i = 0; i < 2; ++i)
            {
                m_HandleNames[i] = string.IsNullOrEmpty(handleName) ? $"Reprojection_{i}" : $"{handleName}_{i}";
            }
            m_CurrentTarget = 0;
        }

        /// <summary>
        /// Allocates or reallocates both ping-pong handles if needed based on the provided descriptor.
        /// Optionally swaps the current target after allocation.
        /// </summary>
        /// <param name="descriptor">The render texture descriptor used for allocation.</param>
        /// <param name="swap">If true, swaps the current target after allocation.</param>
        /// <returns>
        /// <c>true</c> if a handle has been allocated; 
        /// <c>false</c> if no allocation was necessary.
        /// </returns>
        public bool ReAllocateHandlesIfNeeded(Tdesc descriptor, bool swap = false)
        {
            if (m_Handles == null || m_Handles.Length != 2)
            {
                m_Handles = new RTHandle[2];
            }

            bool alloc = false;
            for (int i = 0; i < 2; ++i)
            {
                alloc |= m_ReAllocFunc.Invoke(ref m_Handles[i], descriptor, m_HandleNames[i]);
            }

            if (swap) Swap();
            return alloc;
        }

        /// <summary>
        /// Swaps the current target index to alternate between the two ping-pong handles.
        /// </summary>
        public void Swap()
            => m_CurrentTarget = NextIndex(m_CurrentTarget);

        /// <summary>
        /// Returns the index of the alternate handle in the ping-pong pair.
        /// </summary>
        /// <param name="index">Current index.</param>
        /// <returns>Index of the alternate handle.</returns>
        private static int NextIndex(int index)
            => (index + 1) & 1;
    }

    /// <summary>
    /// Provides per-frame temporal camera data for use in reprojection-based effects
    /// such as TAA, motion reprojection, or temporal denoising.
    /// 
    /// This class manages the previous frame's View-Projection matrix and exposes it
    /// as a global shader property. It guarantees that the update/upload runs only once
    /// per frame, even if called multiple times.
    /// </summary>
    public static class TemporalCameraData
    {
        /// <summary>
        /// Cached previous frame's View-Projection matrix.
        /// Used by shaders to reproject current pixel positions into history buffers.
        /// </summary>
        private static Matrix4x4 s_PrevViewProjMatrix = Matrix4x4.identity;

        /// <summary>
        /// Internal frame counter ensuring that updates occur only once per frame.
        /// </summary>
        private static int s_FrameCount = -1;

        /// <summary>
        /// Shader property ID for setting the previous View-Projection matrix globally.
        /// </summary>
        private static readonly int k_PrevViewProjectionMatrixId =
            Shader.PropertyToID("_Rayforge_Matrix_Prev_VP");

        /// <summary>
        /// Updates the previous View-Projection (VP) matrix once per frame.
        /// 
        /// This method sets the *current cached matrix* into a global shader variable,
        /// and then refreshes the cached matrix with this frame's actual VP matrix.
        /// 
        /// Call this at the start of your rendering frame or before dispatching any
        /// reprojection-reliant shader passes.
        /// </summary>
        public static void UpdatePreviousViewProjectionMatrix()
        {
            int currentFrame = Time.frameCount;

            if (s_FrameCount == currentFrame)
                return;

            s_FrameCount = currentFrame;

            Shader.SetGlobalMatrix(k_PrevViewProjectionMatrixId, s_PrevViewProjMatrix);

            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            s_PrevViewProjMatrix = mainCamera.projectionMatrix * mainCamera.worldToCameraMatrix;
        }
    }
}