using UnityEngine.Rendering;

namespace Rayforge.Core.Common
{
    /// <summary>
    /// Detects which Scriptable Render Pipeline (SRP) is currently active at runtime.
    /// Supports HDRP and URP.
    /// </summary>
    public static class PipelineDetector
    {
        private static bool s_checked = false;
        private static bool s_isHDRP = false;
        private static bool s_isURP = false;

        /// <summary>
        /// Returns true if the High Definition Render Pipeline (HDRP) is active.
        /// </summary>
        /// <returns><c>true</c> if HDRP is active; otherwise, <c>false</c>.</returns>
        public static bool IsHDRP
        {
            get
            {
                EnsureChecked();
                return s_isHDRP;
            }
        }

        /// <summary>
        /// Returns true if the Universal Render Pipeline (URP) is active.
        /// </summary>
        /// <returns><c>true</c> if URP is active; otherwise, <c>false</c>.</returns>
        public static bool IsURP
        {
            get
            {
                EnsureChecked();
                return s_isURP;
            }
        }

        /// <summary>
        /// Forces re-detection of the currently active Scriptable Render Pipeline.
        /// </summary>
        /// <param name="force">
        /// If <c>true</c>, re-checks the active pipeline even if detection was already performed.
        /// </param>
        public static void Detect(bool force = false)
        {
            if (!s_checked || force)
            {
                var rp = GraphicsSettings.currentRenderPipeline;

                s_isHDRP = false;
                s_isURP = false;

                if (rp != null)
                {
                    string name = rp.GetType().Name;

                    if (name.Contains("HDRenderPipeline"))
                        s_isHDRP = true;
                    else if (name.Contains("UniversalRenderPipeline"))
                        s_isURP = true;
                }

                s_checked = true;
            }
        }

        /// <summary>
        /// Ensures that the pipeline detection has been performed before accessing <see cref="IsHDRP"/> or <see cref="IsURP"/>.
        /// </summary>
        private static void EnsureChecked()
        {
            if (!s_checked)
                Detect();
        }
    }
}