using Rayforge.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Rayforge.Common;

namespace Rayforge.Utility.RendererFeatures.DepthPyramid
{
    /// <summary>
    /// ScriptableRendererFeature that generates a hierarchical depth pyramid for use in effects like SSAO or depth-based post-processing.
    /// </summary>
    public class DepthPyramidFeature : ScriptableRendererFeature
    {
        private const string k_ShaderName = "DepthPyramid";
        private const string k_FullShaderName = Globals.CompanyName + "/" + k_ShaderName;

        /// <summary>
        /// The type of input the render pass requires from the camera.
        /// </summary>
        private const ScriptableRenderPassInput k_PassInput = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Color;

        [SerializeField, InspectorName("Injection Point")]
        private RenderPassEvent m_InjectionPoint = RenderPassEvent.AfterRenderingPrePasses;

        /// <summary>
        /// The render pass injection point in the pipeline.  
        /// Setting this property updates the internal render pass event immediately.
        ///
        /// <para>Default is <see cref="RenderPassEvent.AfterRenderingPrePasses"/>.  
        /// This is chosen because it ensures that the depth pyramid is generated **after the camera's pre-passes**, 
        /// but **before main opaque rendering**, making it available for any subsequent effects that rely on depth, 
        /// such as SSAO, depth-based post-processing, or motion vectors.
        /// Otherwise, depth values might be outdated or in an invalid state.
        /// </para>
        /// </summary>
        public RenderPassEvent InjectionPoint
        {
            get => m_InjectionPoint;
            set
            {
                m_InjectionPoint = value;
                if (m_RenderPass != null)
                    m_RenderPass.renderPassEvent = m_InjectionPoint;
            }
        }

        [SerializeField, HideInInspector]
        private ComputeShader m_Shader;

        /// <summary>
        /// Internal instance of the DepthPyramidPass.
        /// </summary>
        private DepthPyramidPass m_RenderPass;

        /// <summary>
        /// Called when the renderer feature is created. Loads the compute shader and initializes the render pass.
        /// </summary>
        public override void Create()
        {
            if (m_Shader == null)
            {
                m_Shader = UnityEngine.Resources.Load<ComputeShader>(k_FullShaderName);
                Assertions.NotNull(m_Shader, "Shader " + k_FullShaderName + " is null");
            }

            if (m_Shader != null)
            {
                m_RenderPass?.Dispose();

                m_RenderPass = new DepthPyramidPass(m_Shader)
                {
                    renderPassEvent = m_InjectionPoint
                };
            }
        }

        /// <summary>
        /// Adds the DepthPyramidPass to the renderer if the camera type is <see cref="CameraType.Game"/>.
        /// Configures the required inputs before enqueueing.
        /// </summary>
        /// <param name="renderer">The ScriptableRenderer instance.</param>
        /// <param name="renderingData">Current rendering data.</param>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_RenderPass == null) return;

            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                m_RenderPass.ConfigureInput(k_PassInput);
                renderer.EnqueuePass(m_RenderPass);
            }
        }

        /// <summary>
        /// Disposes the internal render pass and any unmanaged resources.
        /// </summary>
        /// <param name="disposing">Indicates whether the method is called from Dispose (true) or from a finalizer (false).</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            m_RenderPass?.Dispose();
        }
    }
}
