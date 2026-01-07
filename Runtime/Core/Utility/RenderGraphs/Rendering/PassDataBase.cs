using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Base class for RenderGraph pass input/output configuration and material binding.
    /// </summary>
    public partial class PassDataBase<Tdata, Tdest> : IDisposable
        where Tdata : struct
        where Tdest : struct
    {
        private PassInput m_Source;
        /// <summary>Texture inputs used in the pass.</summary>
        public PassInput Source
        {
            get => m_Source;
            set => m_Source = value;
        }

        private Tdest m_Destination;
        /// <summary>Texture that this pass writes into.</summary>
        public Tdest Destination
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
        public void CopyFrom(PassDataBase<Tdata, Tdest> other)
        {
            m_Destination = other.m_Destination;
            m_AdditionalData = other.m_AdditionalData;
        }

        /// <summary>
        /// Enumerates all valid (non-null) input textures.
        /// </summary>
        public IEnumerable<TexturePassMeta> PassInput
        {
            get
            {
                foreach (var input in m_Source.Input)
                    yield return input;
            }
        }
    }
}