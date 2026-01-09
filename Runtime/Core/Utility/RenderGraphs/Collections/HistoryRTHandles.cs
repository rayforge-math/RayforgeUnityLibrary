using Rayforge.Core.Common;
using Rayforge.Core.Diagnostics;
using Rayforge.Core.Rendering.Collections;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.Core.Utility.RenderGraphs.Collections
{
    /// <summary>
    /// Manages a pair of persistent render targets (history handles) for frame-over-frame operations.
    /// One handle represents the current target (write), the other holds the previous frame's data (read).
    /// Suitable for temporal effects like reprojection, motion blur, or any frame-history dependent process.
    /// </summary>
    /// <typeparam name="TData">
    /// Optional user-defined context passed to the allocation function. Useful for providing external resources,
    /// a render graph context, or any other data required during allocation without capturing from the surrounding scope.
    /// </typeparam>
    public class HistoryRTHandles<TData> : HistoryHandles<RTHandle>
    {
        /// <summary>
        /// Function signature for creating or reallocating a texture handle.
        /// </summary>
        /// <param name="handle">The handle to create or reallocate.</param>
        /// <param name="descriptor">The render texture descriptor used for allocation.</param>
        /// <param name="name">Optional name for debugging/profiling.</param>
        /// <param name="data">Optional user-provided context for allocation logic.</param>
        /// <returns><c>true</c> if a handle was allocated/reallocated, <c>false</c> otherwise.</returns>
        public delegate bool TextureReAllocFunction(ref RTHandle handle, RenderTextureDescriptor descriptor, string name, TData data = default);

        private string[] m_HandleNames;
        private TextureReAllocFunction m_ReAllocFunc;

        private const string k_DefaultHandleName = "HistoryHandle";

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRTHandles{TData}"/>.
        /// </summary>
        /// <param name="reAllocFunc">Delegate used to create or reallocate handles.</param>
        /// <param name="initial0">Initial first handle (current).</param>
        /// <param name="initial1">Initial second handle (history).</param>
        /// <param name="handleName">Optional base name for debugging/profiling.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="reAllocFunc"/> is <c>null</c>.</exception>
        public HistoryRTHandles(TextureReAllocFunction reAllocFunc, RTHandle initial0, RTHandle initial1, string handleName = null)
            : base(initial0, initial1)
        {
            if (reAllocFunc == null)
                throw new ArgumentNullException(nameof(reAllocFunc));

            m_ReAllocFunc = reAllocFunc;

            m_HandleNames = new string[2];
            for (int i = 0; i < 2; ++i)
            {
                m_HandleNames[i] = string.IsNullOrEmpty(handleName) ? $"{k_DefaultHandleName}_{i}" : $"{handleName}_{i}";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRTHandles{TData}"/>.
        /// The handles are initially null; allocation is expected to be done later via <see cref="ReAllocateHandlesIfNeeded"/>.
        /// </summary>
        /// <param name="reAllocFunc">Delegate used to create or reallocate handles.</param>
        /// <param name="handleName">Optional base name for debugging/profiling.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="reAllocFunc"/> is <c>null</c>.</exception>
        public HistoryRTHandles(TextureReAllocFunction reAllocFunc, string handleName)
            : this(reAllocFunc, null, null, handleName)
        { }

        /// <summary>
        /// Allocates or reallocates both ping-pong handles if needed based on the provided descriptor.
        /// Only updates handles that were actually reallocated.
        /// Optionally swaps the current target after allocation.
        /// </summary>
        /// <param name="descriptor">The render texture descriptor used for allocation.</param>
        /// <param name="swap">If true, swaps the current and previous handle after allocation.</param>
        /// <param name="data">Optional user-defined context passed to the allocation function.</param>
        /// <returns><c>true</c> if at least one handle was allocated/reallocated, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="descriptor"/> has non-positive width or height.
        /// </exception>
        public bool ReAllocateHandlesIfNeeded(RenderTextureDescriptor descriptor, bool swap = false, TData data = default)
        {
            if (descriptor.width <= 0 || descriptor.height <= 0)
                throw new ArgumentException("RenderTextureDescriptor must have positive width and height.", nameof(descriptor));

            bool alloc = false;

            for (int i = 0; i < 2; ++i)
            {
                var handle = m_Handles[i];
                if (m_ReAllocFunc.Invoke(ref handle, descriptor, m_HandleNames[i], data))
                {
                    m_Handles[i] = handle;
                    alloc |= true;
                }
            }

            if (swap) Swap();
            return alloc;
        }
    }

    /// <summary>
    /// Manages a pair of persistent render targets (history handles) for frame-over-frame operations.
    /// One handle represents the current target (write), the other holds the previous frame's data (read).
    /// Suitable for temporal effects like reprojection, motion blur, or any frame-history dependent process.
    /// </summary>
    /// <typeparam name="TData">
    /// Optional user-defined context passed to the allocation function. Useful for providing external resources,
    /// a render graph context, or any other data required during allocation without capturing from the surrounding scope.
    /// </typeparam>
    public class HistoryRTHandles : HistoryRTHandles<NoData>
    {
        /// <summary>
        /// Function signature for creating or reallocating a texture handle.
        /// </summary>
        /// <param name="handle">The handle to create or reallocate.</param>
        /// <param name="descriptor">The render texture descriptor used for allocation.</param>
        /// <param name="name">Optional name for debugging/profiling.</param>
        /// <returns><c>true</c> if a handle was allocated/reallocated, <c>false</c> otherwise.</returns>
        public delegate bool TextureReAllocFunctionNoParam(ref RTHandle handle, RenderTextureDescriptor descriptor, string name);

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRTHandles"/>.
        /// </summary>
        /// <param name="reAllocFunc">Delegate used to create or reallocate handles.</param>
        /// <param name="initial0">Initial first handle (current).</param>
        /// <param name="initial1">Initial second handle (history).</param>
        /// <param name="handleName">Optional base name for debugging/profiling.</param>
        public HistoryRTHandles(TextureReAllocFunctionNoParam reAllocFunc, RTHandle initial0, RTHandle initial1, string handleName = null)
            : base((ref RTHandle handle, RenderTextureDescriptor descriptor, string name, NoData _) =>
            {
                return reAllocFunc.Invoke(ref handle, descriptor, name);
            },
            initial0, 
            initial1, 
            handleName)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryRTHandles"/>.
        /// The handles are initially null; allocation is expected to be done later via <see cref="ReAllocateHandlesIfNeeded"/>.
        /// </summary>
        /// <param name="reAllocFunc">Delegate used to create or reallocate handles.</param>
        /// <param name="handleName">Optional base name for debugging/profiling.</param>
        public HistoryRTHandles(TextureReAllocFunctionNoParam reAllocFunc, string handleName)
            : this(reAllocFunc, null, null, handleName)
        { }
    }
}