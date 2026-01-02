using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.ShaderExtensions.Pipeline
{
    /// <summary>
    /// Provides runtime detection of the active Scriptable Render Pipeline (URP or HDRP)
    /// and applies matching global shader keywords.
    /// 
    /// This allows shaders to use:
    /// <code>
    /// #if defined(RAYFORGE_PIPELINE_HDRP)
    ///     // HDRP path
    /// #elif defined(RAYFORGE_PIPELINE_URP)
    ///     // URP path
    /// #endif
    /// </code>
    /// </summary>
    public static class PipelineDefines
    {
        /// <summary>
        /// Global shader keyword used when the High Definition Render Pipeline (HDRP) is active.
        /// </summary>
        public static string HdrpKeyword => k_HdrpKeyword;
        private static readonly string k_HdrpKeyword = "RAYFORGE_PIPELINE_HDRP";

        /// <summary>
        /// Global shader keyword used when the Universal Render Pipeline (URP) is active.
        /// </summary>
        public static string UrpKeyword => k_UrpKeyword;
        private static readonly string k_UrpKeyword = "RAYFORGE_PIPELINE_URP";

        /// <summary>
        /// True if the currently active render pipeline is HDRP.
        /// </summary>
        public static bool IsHDRP
        {
            get
            {
                DetectPipeline();
                return s_isHDRP;
            }
        }
        private static bool s_isHDRP = false;

        /// <summary>
        /// True if the currently active render pipeline is URP.
        /// </summary>
        public static bool IsURP
        {
            get
            {
                DetectPipeline();
                return s_isURP;
            }
        }
        private static bool s_isURP = false;

        /// <summary>
        /// Tracks whether pipeline detection has already been performed.
        /// </summary>
        private static bool s_PipelineChecked = false;

        /// <summary>
        /// Detects whether URP or HDRP is currently in use and applies matching shader keywords.
        /// Call <c>DetectPipeline(true)</c> to force re-checking.
        /// </summary>
        /// <param name="force">If true, forces re-detection even if already checked.</param>
        public static void DetectPipeline(bool force = false)
        {
            if (!s_PipelineChecked || force)
            {
                var rp = GraphicsSettings.currentRenderPipeline;

                bool isURP = false;
                bool isHDRP = false;

                if (rp != null)
                {
                    string name = rp.GetType().Name;

                    if (name.Contains("HDRenderPipeline"))
                        isHDRP = true;
                    else if (name.Contains("UniversalRenderPipeline"))
                        isURP = true;
                }

                s_isHDRP = isHDRP;
                s_isURP = isURP;

                // Apply shader keywords
                if (s_isHDRP)
                {
                    Shader.EnableKeyword(k_HdrpKeyword);
                    Shader.DisableKeyword(k_UrpKeyword);
                }
                else if (s_isURP)
                {
                    Shader.EnableKeyword(k_UrpKeyword);
                    Shader.DisableKeyword(k_HdrpKeyword);
                }
                else
                {
                    Shader.DisableKeyword(k_HdrpKeyword);
                    Shader.DisableKeyword(k_UrpKeyword);
                }

                s_PipelineChecked = true;
            }
        }
    }
}