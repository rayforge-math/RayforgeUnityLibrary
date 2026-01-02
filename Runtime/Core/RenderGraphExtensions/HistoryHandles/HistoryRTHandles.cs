using Rayforge.Rendering.HistoryHandles;
using UnityEngine;
using UnityEngine.Rendering;

using static Rayforge.Utility.RuntimeCheck.Asserts;

namespace Rayforge.RenderGraphExtensions.HistoryHandles
{
    /// <summary>
    /// Manages a pair of persistent render targets (history handles) for frame-over-frame operations.
    /// One handle represents the current target (write), the other holds the previous frame's data (read).
    /// Suitable for temporal effects like reprojection, motion blur, or any frame-history dependent process.
    /// </summary>
    public class HistoryRTHandles : HistoryHandles<RTHandle>
    {
        /// <summary>
        /// Function signature for creating or reallocating a texture handle.
        /// </summary>
        /// <param name="handle">The handle to create or reallocate.</param>
        /// <param name="descriptor">The render texture descriptor used for allocation.</param>
        /// <param name="name">Optional name for debugging/profiling.</param>
        /// <returns><c>true</c> if a handle was allocated/reallocated, <c>false</c> otherwise.</returns>
        public delegate bool TextureReAllocFunction(ref RTHandle handle, RenderTextureDescriptor descriptor, string name);

        private string[] m_HandleNames;
        private TextureReAllocFunction m_ReAllocFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderGraphHistoryHandles"/> struct.
        /// </summary>
        /// <param name="reAllocFunc">Delegate used to create or reallocate handles.</param>
        /// <param name="initial0">Initial first handle (current).</param>
        /// <param name="initial1">Initial second handle (history).</param>
        /// <param name="handleName">Optional base name for debugging/profiling.</param>
        public HistoryRTHandles(TextureReAllocFunction reAllocFunc, RTHandle initial0, RTHandle initial1, string handleName = "")
            : base(initial0, initial1)
        {
            ValidateDelegate(reAllocFunc);
            m_ReAllocFunc = reAllocFunc;

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

            for(int i = 0; i < 2; ++i)
            {
                var handle = m_Handles[i];
                if (m_ReAllocFunc.Invoke(ref handle, descriptor, m_HandleNames[i]))
                {
                    m_Handles[i] = handle;
                    alloc |= true;
                }
            }

            if (swap) Swap();
            return alloc;
        }
    }
}