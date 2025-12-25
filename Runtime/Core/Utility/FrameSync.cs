using UnityEngine;

namespace Rayforge.Utility.FrameSync
{
    /// <summary>
    /// Lightweight per-frame execution guard.
    /// Ensures a code path runs at most once per rendered frame.
    ///
    /// Intended for systems that may be invoked multiple times per frame
    /// but must update state or upload data only once.
    /// </summary>
    /// <remarks>
    /// Each system must own its own instance.
    /// This struct holds no global state and relies on <see cref="Time.frameCount"/>.
    ///
    /// Do not copy this struct or pass it by value.
    /// Store it as a persistent field.
    /// </remarks>
    public struct FrameOnce
    {
        private int _lastFrame;

        /// <summary>
        /// Returns true if this is the first call in the current frame.
        /// </summary>
        public bool TryBegin()
        {
            int frame = Time.frameCount;
            if (_lastFrame == frame)
                return false;

            _lastFrame = frame;
            return true;
        }
    }
}