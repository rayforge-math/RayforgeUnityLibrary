using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Base class for RenderGraph pass input/output configuration and pass metadata.
    /// Supports up to 8 input textures directly.
    /// </summary>
    /// <typeparam name="Tmeta">Type of the pass metadata (e.g., compute or raster meta).</typeparam>
    /// <typeparam name="Tdest">Type of the destination output texture.</typeparam>
    public partial class PassDataBase<Tmeta, Tdest> : IDisposable
        where Tmeta : struct
        where Tdest : struct
    {
        // --- Fixed input slots ---
        private TextureMeta _input0;
        private TextureMeta _input1;
        private TextureMeta _input2;
        private TextureMeta _input3;
        private TextureMeta _input4;
        private TextureMeta _input5;
        private TextureMeta _input6;
        private TextureMeta _input7;

        /// <summary>
        /// Maximum number of supported input textures.
        /// </summary>
        public const int InputCapacity = 8;

        private Tdest m_Destination;
        /// <summary>
        /// Destination texture that this pass writes into.
        /// </summary>
        public Tdest Destination
        {
            get => m_Destination;
            set => m_Destination = value;
        }

        private Tmeta m_PassMeta;
        /// <summary>
        /// Metadata describing this pass (e.g., shader, kernel, material, etc.).
        /// </summary>
        public Tmeta PassMeta
        {
            get => m_PassMeta;
            set => m_PassMeta = value;
        }

        /// <summary>
        /// Releases any allocated resources. Override in derived types if needed.
        /// </summary>
        public virtual void Dispose() { }

        /// <summary>
        /// Copies all configuration values from another pass.
        /// </summary>
        /// <param name="other">The pass data to copy values from.</param>
        public void CopyFrom(PassDataBase<Tmeta, Tdest> other)
        {
            for (int i = 0; i < InputCapacity; i++)
            {
                SetInput(i, other.GetInput(i));
            }

            m_Destination = other.m_Destination;
            m_PassMeta = other.m_PassMeta;
        }

        /// <summary>
        /// Sets the input texture at the specified index.
        /// </summary>
        /// <param name="index">Input index (0-based, max 7).</param>
        /// <param name="input">The texture metadata to assign to the input slot.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
        public void SetInput(int index, TextureMeta input)
        {
            switch (index)
            {
                case 0: _input0 = input; break;
                case 1: _input1 = input; break;
                case 2: _input2 = input; break;
                case 3: _input3 = input; break;
                case 4: _input4 = input; break;
                case 5: _input5 = input; break;
                case 6: _input6 = input; break;
                case 7: _input7 = input; break;
                default: throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range for PassDataBase (0-{InputCapacity - 1}).");
            }
        }

        /// <summary>
        /// Convenience method to set an input using property ID and texture handle at a specific index.
        /// </summary>
        /// <param name="index">Input index (0-based).</param>
        /// <param name="propertyId">Shader property ID of the input.</param>
        /// <param name="handle">RenderGraph texture handle.</param>
        public void SetInput(int index, int propertyId, TextureHandle handle)
            => SetInput(index, new TextureMeta { propertyId = propertyId, handle = handle });

        /// <summary>
        /// Convenience method to set the first input (index 0) using property ID and texture handle.
        /// </summary>
        /// <param name="propertyId">Shader property ID of the input.</param>
        /// <param name="handle">RenderGraph texture handle.</param>
        public void SetInput(int propertyId, TextureHandle handle)
            => SetInput(0, new TextureMeta { propertyId = propertyId, handle = handle });

        /// <summary>
        /// Sets the destination texture for this pass.
        /// </summary>
        /// <param name="destination">The texture metadata to write into.</param>
        public void SetDestination(Tdest destination)
        {
            m_Destination = destination;
        }

        /// <summary>
        /// Gets the input texture at the specified index.
        /// </summary>
        /// <param name="index">Input index (0-based).</param>
        /// <returns>The texture metadata stored at the given index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
        public TextureMeta GetInput(int index)
        {
            return index switch
            {
                0 => _input0,
                1 => _input1,
                2 => _input2,
                3 => _input3,
                4 => _input4,
                5 => _input5,
                6 => _input6,
                7 => _input7,
                _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range for PassDataBase (0-{InputCapacity - 1})."),
            };
        }

        /// <summary>
        /// Enumerates all valid (non-null) input textures.
        /// </summary>
        /// <returns>An enumerable of valid <see cref="TextureMeta"/> inputs.</returns>
        public IEnumerable<TextureMeta> PassInput
        {
            get
            {
                for (int i = 0; i < InputCapacity; i++)
                {
                    var input = GetInput(i);
                    if (input.handle.IsValid())
                        yield return input;
                }
            }
        }
    }
}
