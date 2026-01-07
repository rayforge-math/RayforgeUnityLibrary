using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Base class for RenderGraph pass input/output configuration and material binding.
    /// </summary>
    public abstract class RenderPassDataBase<Tdata> : IDisposable
        where Tdata : struct
    {
        private TextureHandle m_Destination;
        /// <summary>Texture that this pass writes into.</summary>
        public TextureHandle Destination
        {
            get => m_Destination;
            set => m_Destination = value;
        }

        private Tdata m_AdditionalData;
        /// <summary>Custom additional data passed to the <see cref="BaseRenderFunc{PassData,ContextType}">.</summary>
        public Tdata AdditionalData
        {
            get => m_AdditionalData;
            set => m_AdditionalData = value;
        }

        /// <summary>
        /// Releases any allocated resources. Override in derived types if needed.
        /// </summary>
        public virtual void Dispose() { }

        /// <summary>
        /// Copies pass configuration values from another pass.
        /// </summary>
        public virtual void CopyFrom(RenderPassDataBase<Tdata> other)
        {
            m_Destination = other.m_Destination;
            m_AdditionalData = other.m_AdditionalData;
        }

        /// <summary>
        /// Enumerates all valid texture inputs used by this pass.
        /// </summary>
        public abstract IEnumerable<RenderPassTexture> PassInput { get; }
    }
}