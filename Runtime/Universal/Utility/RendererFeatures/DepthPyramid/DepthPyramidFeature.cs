using Rayforge.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Rayforge.Common;

namespace Rayforge.Utility.RendererFeatures.DepthPyramid
{
    public class DepthPyramidFeature : ScriptableRendererFeature
    {
        private const string k_ShaderName = "DepthPyramid";
        private const string k_FullShaderName = Globals.CompanyName + "/" + k_ShaderName;
        private const ScriptableRenderPassInput k_PassInput = ScriptableRenderPassInput.Depth;

        [SerializeField, InspectorName("Injection Point")]
        private RenderPassEvent m_InjectionPoint = RenderPassEvent.AfterRenderingPrePasses;
        public RenderPassEvent InjectionPoint
        {
            get { return m_InjectionPoint; }
            set
            {
                m_InjectionPoint = value;
                m_RenderPass.renderPassEvent = m_InjectionPoint;
            }
        }

        [SerializeField, HideInInspector]
        private ComputeShader m_Shader;
        private DepthPyramidPass m_RenderPass;

        public override void Create()
        {
            if (m_Shader == null)
            {
                m_Shader = UnityEngine.Resources.Load<ComputeShader>(k_FullShaderName);
                Assertions.NotNull(m_Shader, "Shader " + k_FullShaderName + " is null");
            }

            if (m_Shader != null)
            {
                if (m_RenderPass != null)
                    m_RenderPass.Dispose();

                m_RenderPass = new DepthPyramidPass(m_Shader);
                m_RenderPass.renderPassEvent = m_InjectionPoint;
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_RenderPass == null) return;

            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                m_RenderPass.ConfigureInput(k_PassInput);
                renderer.EnqueuePass(m_RenderPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            m_RenderPass.Dispose();
        }
    }
}
