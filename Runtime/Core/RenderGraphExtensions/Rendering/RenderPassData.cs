using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.RenderGraphExtensions.Rendering
{
    /// <summary>
    /// Base class for RenderGraph pass input/output configuration and material binding.
    /// </summary>
    public abstract class RenderPassData : IDisposable
    {
        private TextureHandle m_Destination;
        /// <summary>Texture that this pass writes into.</summary>
        public TextureHandle Destination
        {
            get => m_Destination;
            set => m_Destination = value;
        }

        private Material m_Material;
        /// <summary>Material used when drawing the pass.</summary>
        public Material Material
        {
            get => m_Material;
            set => m_Material = value;
        }

        private MaterialPropertyBlock m_PropertyBlock;
        /// <summary>Material property overrides applied when drawing. Optional.</summary>
        public MaterialPropertyBlock PropertyBlock
        {
            get => m_PropertyBlock;
            set => m_PropertyBlock = value;
        }

        private int m_PassId;
        /// <summary>Material pass index to execute.</summary>
        public int PassId
        {
            get => m_PassId;
            set => m_PassId = value;
        }

        /// <summary>
        /// Releases any allocated resources. Override in derived types if needed.
        /// </summary>
        public virtual void Dispose() { }

        /// <summary>
        /// Copies pass configuration values from another pass.
        /// </summary>
        public virtual void Copy(RenderPassData other)
        {
            m_Destination = other.m_Destination;
            m_Material = other.m_Material;
            m_PropertyBlock = other.m_PropertyBlock;
            m_PassId = other.m_PassId;
        }

        /// <summary>
        /// Enumerates all valid texture inputs used by this pass.
        /// </summary>
        public abstract IEnumerable<RenderPassTexture> PassInput { get; }
    }
}