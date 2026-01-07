using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Struct-based container for multiple texture inputs for a RenderGraph pass.
    /// Avoids heap allocations by storing inputs as fixed fields.
    /// Supports up to 8 input textures.
    /// </summary>
    public struct PassInput
    {
        // --- Fixed input slots ---
        private TexturePassMeta _input0;
        private TexturePassMeta _input1;
        private TexturePassMeta _input2;
        private TexturePassMeta _input3;
        private TexturePassMeta _input4;
        private TexturePassMeta _input5;
        private TexturePassMeta _input6;
        private TexturePassMeta _input7;

        /// <summary>
        /// Maximum number of supported input textures in this struct.
        /// </summary>
        public const int Capacity = 8;

        /// <summary>
        /// Sets the input at the specified index.
        /// </summary>
        public void SetInput(int index, TexturePassMeta input)
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
                default: throw new System.ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range for PassInput (0-{Capacity - 1}).");
            }
        }

        /// <summary>
        /// Convenience method to set an input using property ID and handle at a specific index.
        /// </summary>
        public void SetInput(int index, int propertyId, TextureHandle handle)
            => SetInput(index, new TexturePassMeta { propertyId = propertyId, handle = handle });

        /// <summary>
        /// Convenience method to set the first input using property ID and handle.
        /// </summary>
        public void SetInput(int propertyId, TextureHandle handle)
            => SetInput(0, new TexturePassMeta { propertyId = propertyId, handle = handle });

        /// <summary>
        /// Gets the input at the specified index.
        /// </summary>
        public TexturePassMeta GetInput(int index)
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
                _ => throw new System.ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range for PassInput (0-{Capacity - 1})."),
            };
        }

        /// <summary>
        /// Enumerates all valid (non-null) input textures.
        /// </summary>
        public IEnumerable<TexturePassMeta> Input
        {
            get
            {
                for (int i = 0; i < Capacity; i++)
                {
                    var input = GetInput(i);
                    if (input.handle.IsValid())
                        yield return input;
                }
            }
        }
    }
}
