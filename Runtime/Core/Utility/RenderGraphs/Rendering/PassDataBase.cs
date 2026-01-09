using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.Core.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Base class for RenderGraph pass input/output configuration and pass metadata.
    /// Supports up to 8 input textures directly.
    /// </summary>
    /// <typeparam name="TDerived">Type of the derived class (CRTP).</typeparam>
    /// <typeparam name="TMeta">Type of the pass metadata (e.g., compute or raster meta).</typeparam>
    /// <typeparam name="TDest">Type of the destination output texture.</typeparam>
    public abstract class PassDataBase<TDerived, TMeta, TDest> : IDisposable
        where TDerived : PassDataBase<TDerived, TMeta, TDest>
        where TMeta : struct
        where TDest : struct
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

        private TDest m_Destination;
        /// <summary>
        /// Gets or sets the destination texture that this pass writes into.
        /// </summary>
        public TDest Destination
        {
            get => m_Destination;
            set => m_Destination = value;
        }

        private TMeta m_PassMeta;
        /// <summary>
        /// Gets or sets the metadata describing this pass (e.g., shader, kernel, material).
        /// </summary>
        public TMeta PassMeta
        {
            get => m_PassMeta;
            set => m_PassMeta = value;
        }

        /// <summary>
        /// Releases any allocated resources. Override in derived types if needed.
        /// </summary>
        public virtual void Dispose() { }

        /// <summary>
        /// Copies all configuration values from another pass data instance.
        /// </summary>
        /// <param name="other">The pass data to copy from.</param>
        public void CopyFrom(TDerived other)
        {
            for (int i = 0; i < InputCapacity; ++i)
            {
                SetInput(i, other.GetInput(i));
            }

            m_Destination = other.m_Destination;
            m_PassMeta = other.m_PassMeta;

            CopyUserData(other);
        }

        /// <summary>
        /// Override this in a derived pass data type to copy any additional fields.
        /// If not overridden, these fields will not be present during render pass dispatch.
        /// </summary>
        /// <remarks>
        /// The precise derived type <see cref="TDerived"/> is known at compile time,
        /// so the compiler can resolve this call directly without using a vtable.
        /// </remarks>
        public abstract void CopyUserData(TDerived other);

        /// <summary>
        /// Sets the input texture at the specified index.
        /// </summary>
        /// <param name="index">Zero-based input index (0-7).</param>
        /// <param name="input">The texture metadata to assign to the input slot.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range [0-7].</exception>
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
                default: throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range (must be 0-{InputCapacity - 1}).");
            }
        }

        /// <summary>
        /// Sets an input texture at the specified index using a shader property ID and texture handle.
        /// </summary>
        /// <param name="index">Zero-based input index (0-7).</param>
        /// <param name="propertyId">Shader property ID to bind the texture to.</param>
        /// <param name="handle">RenderGraph texture handle.</param>
        public void SetInput(int index, int propertyId, TextureHandle handle)
            => SetInput(index, new TextureMeta { propertyId = propertyId, handle = handle });

        /// <summary>
        /// Sets the first input texture (index 0) using a shader property ID and texture handle.
        /// </summary>
        /// <param name="propertyId">Shader property ID to bind the texture to.</param>
        /// <param name="handle">RenderGraph texture handle.</param>
        public void SetInput(int propertyId, TextureHandle handle)
            => SetInput(0, new TextureMeta { propertyId = propertyId, handle = handle });

        /// <summary>
        /// Sets the destination texture for this pass.
        /// </summary>
        /// <param name="destination">The destination texture metadata.</param>
        public void SetDestination(TDest destination)
        {
            m_Destination = destination;
        }

        /// <summary>
        /// Gets the input texture metadata at the specified index.
        /// </summary>
        /// <param name="index">Zero-based input index (0-7).</param>
        /// <returns>The texture metadata stored at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range [0-7].</exception>
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
                _ => throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range (must be 0-{InputCapacity - 1})."),
            };
        }

        /// <summary>
        /// Gets an enumerable of all valid (non-null) input textures assigned to this pass.
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
