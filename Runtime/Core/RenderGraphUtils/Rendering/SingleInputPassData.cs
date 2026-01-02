using Rayforge.ShaderExtensions.Blitter;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.RenderGraphUtils.Rendering
{
    /// <summary>
    /// Pass data container for passes that consume exactly one texture input.
    /// </summary>
    public class SingleInputPassData : RenderPassData
    {
        private RenderPassTexture m_Input;

        /// <summary>Returns the configured input texture.</summary>
        public RenderPassTexture Input => m_Input;

        /// <summary>Assigns the input texture using the default blit texture property.</summary>
        public void SetInput(TextureHandle handle)
            => SetInput(new RenderPassTexture { handle = handle, propertyId = BlitParameters.BlitTextureId });

        /// <summary>Assigns the input texture using a custom shader property ID.</summary>
        public void SetInput(int propertyId, TextureHandle handle)
            => SetInput(new RenderPassTexture { handle = handle, propertyId = propertyId });

        /// <summary>Assigns the input texture.</summary>
        public void SetInput(RenderPassTexture input)
            => m_Input = input;

        /// <inheritdoc/>
        public override IEnumerable<RenderPassTexture> PassInput
        {
            get { yield return m_Input; }
        }
    }
}