using Rayforge.Utility.PingPongBuffer;
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
    /// Manages a pair of persistent render targets (history handles) for frame-over-frame operations.
    /// One handle represents the current target (write), the other holds the previous frame's data (read).
    /// Suitable for temporal effects like reprojection, motion blur, or any frame-history dependent process.
    /// </summary>
    public struct HistoryHandles
    {
        /// <summary>
        /// Function signature for creating or reallocating a texture handle.
        /// </summary>
        /// <param name="handle">The handle to create or reallocate.</param>
        /// <param name="descriptor">The render texture descriptor used for allocation.</param>
        /// <param name="name">Optional name for debugging/profiling.</param>
        /// <returns><c>true</c> if a handle was allocated/reallocated, <c>false</c> otherwise.</returns>
        public delegate bool TextureReAllocFunction(ref RTHandle handle, RenderTextureDescriptor descriptor, string name);

        private PingPongBuffer<RTHandle> m_Buffer;
        private string[] m_HandleNames;
        private TextureReAllocFunction m_ReAllocFunc;

        /// <summary>
        /// Gets the handle used as the current frame's target (write).
        /// </summary>
        public RTHandle Target => m_Buffer.First;

        /// <summary>
        /// Gets the handle containing the previous frame's data (read).
        /// </summary>
        public RTHandle History => m_Buffer.Second;

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryHandles"/> struct.
        /// </summary>
        /// <param name="reAllocFunc">Delegate used to create or reallocate handles.</param>
        /// <param name="initial0">Initial first handle (current).</param>
        /// <param name="initial1">Initial second handle (history).</param>
        /// <param name="handleName">Optional base name for debugging/profiling.</param>
        public HistoryHandles(TextureReAllocFunction reAllocFunc, RTHandle initial0, RTHandle initial1, string handleName = "")
        {
            ValidateDelegate(reAllocFunc);
            m_ReAllocFunc = reAllocFunc;
            m_Buffer = new PingPongBuffer<RTHandle>(initial0, initial1);

            m_HandleNames = new string[2];
            for (int i = 0; i < 2; ++i)
            {
                m_HandleNames[i] = string.IsNullOrEmpty(handleName) ? $"HistoryHandle_{i}" : $"{handleName}_{i}";
            }
        }

        /// <summary>
        /// Allocates or reallocates both ping-pong handles if needed based on the provided descriptor.
        /// Only updates handles that were actually reallocated.
        /// Optionally swaps the current target after allocation.
        /// </summary>
        /// <param name="descriptor">The render texture descriptor used for allocation.</param>
        /// <param name="swap">If true, swaps the current and previous handle after allocation.</param>
        /// <returns><c>true</c> if at least one handle was allocated/reallocated, <c>false</c> otherwise.</returns>
        public bool ReAllocateHandlesIfNeeded(RenderTextureDescriptor descriptor, bool swap = false)
        {
            bool alloc = false;

            // Reallocate first handle
            var handle0 = m_Buffer.First;
            if (m_ReAllocFunc.Invoke(ref handle0, descriptor, m_HandleNames[0]))
            {
                m_Buffer.SetFirst(handle0);
                alloc = true;
            }

            // Reallocate second handle
            var handle1 = m_Buffer.Second;
            if (m_ReAllocFunc.Invoke(ref handle1, descriptor, m_HandleNames[1]))
            {
                m_Buffer.SetSecond(handle1);
                alloc = true;
            }

            if (swap) m_Buffer.Swap();
            return alloc;
        }
    }
}