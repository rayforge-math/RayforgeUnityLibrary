using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.RenderGraphExtensions.Rendering
{
    /// <summary>
    /// Base class for passes supporting multiple texture inputs.
    /// </summary>
    public abstract class MultiInputPassData : RenderPassData
    {
        /// <summary>Maximum number of supported input textures.</summary>
        public abstract int Capacity { get; }

        /// <summary>Assigns an input texture to the specified index.</summary>
        protected abstract void SetInput(int index, RenderPassTexture input);

        /// <summary>Assigns an input texture to the specified index.</summary>
        public void SetInput(int index, int propertyId, TextureHandle handle)
            => SetInput(index, new RenderPassTexture { propertyId = propertyId, handle = handle });

        /// <summary>Returns the input texture stored at the given index.</summary>
        public abstract RenderPassTexture GetInput(int index);

        /// <inheritdoc/>
        /// <remarks>
        /// We avoid using an array here because Unity retrieves PassData objects from an internal pool.
        /// Creating or copying an array would immediately trigger heap allocations and garbage collection,
        /// which we want to avoid during rendering.
        /// </remarks>
        public override IEnumerable<RenderPassTexture> PassInput
        {
            get
            {
                for (int i = 0; i < Capacity; ++i)
                {
                    var input = GetInput(i);
                    if (input.handle.IsValid())
                        yield return input;
                }
            }
        }
    }

    /// <summary>Two-input pass variant.</summary>
    public class MultiInputPassData2 : MultiInputPassData
    {
        private const int k_Capacity = 2;
        public override int Capacity => k_Capacity;

        private RenderPassTexture m_Input0;
        private RenderPassTexture m_Input1;

        protected override void SetInput(int index, RenderPassTexture input)
        {
            switch (index)
            {
                case 0: m_Input0 = input; break;
                case 1: m_Input1 = input; break;
                default: throw new IndexOutOfRangeException();
            }
        }

        public override RenderPassTexture GetInput(int index)
        {
            switch (index)
            {
                case 0: return m_Input0;
                case 1: return m_Input1;
                default: throw new IndexOutOfRangeException();
            }
        }
    }

    /// <summary>Three-input pass variant.</summary>
    public class MultiInputPassData3 : MultiInputPassData
    {
        private const int k_Capacity = 3;
        public override int Capacity => k_Capacity;

        private RenderPassTexture m_Input0;
        private RenderPassTexture m_Input1;
        private RenderPassTexture m_Input2;

        protected override void SetInput(int index, RenderPassTexture input)
        {
            switch (index)
            {
                case 0: m_Input0 = input; break;
                case 1: m_Input1 = input; break;
                case 2: m_Input2 = input; break;
                default: throw new IndexOutOfRangeException();
            }
        }

        public override RenderPassTexture GetInput(int index)
        {
            switch (index)
            {
                case 0: return m_Input0;
                case 1: return m_Input1;
                case 2: return m_Input2;
                default: throw new IndexOutOfRangeException();
            }
        }
    }

    /// <summary>Four-input pass variant.</summary>
    public class MultiInputPassData4 : MultiInputPassData
    {
        private const int k_Capacity = 4;
        public override int Capacity => k_Capacity;

        private RenderPassTexture m_Input0;
        private RenderPassTexture m_Input1;
        private RenderPassTexture m_Input2;
        private RenderPassTexture m_Input3;

        protected override void SetInput(int index, RenderPassTexture input)
        {
            switch (index)
            {
                case 0: m_Input0 = input; break;
                case 1: m_Input1 = input; break;
                case 2: m_Input2 = input; break;
                case 3: m_Input3 = input; break;
                default: throw new IndexOutOfRangeException();
            }
        }

        public override RenderPassTexture GetInput(int index)
        {
            switch (index)
            {
                case 0: return m_Input0;
                case 1: return m_Input1;
                case 2: return m_Input2;
                case 3: return m_Input3;
                default: throw new IndexOutOfRangeException();
            }
        }
    }

    /// <summary>Five-input pass variant.</summary>
    public class MultiInputPassData5 : MultiInputPassData
    {
        private const int k_Capacity = 5;
        public override int Capacity => k_Capacity;

        private RenderPassTexture m_Input0;
        private RenderPassTexture m_Input1;
        private RenderPassTexture m_Input2;
        private RenderPassTexture m_Input3;
        private RenderPassTexture m_Input4;

        protected override void SetInput(int index, RenderPassTexture input)
        {
            switch (index)
            {
                case 0: m_Input0 = input; break;
                case 1: m_Input1 = input; break;
                case 2: m_Input2 = input; break;
                case 3: m_Input3 = input; break;
                case 4: m_Input4 = input; break;
                default: throw new IndexOutOfRangeException();
            }
        }

        public override RenderPassTexture GetInput(int index)
        {
            switch (index)
            {
                case 0: return m_Input0;
                case 1: return m_Input1;
                case 2: return m_Input2;
                case 3: return m_Input3;
                case 4: return m_Input4;
                default: throw new IndexOutOfRangeException();
            }
        }
    }

    /// <summary>Six-input pass variant.</summary>
    public class MultiInputPassData6 : MultiInputPassData
    {
        private const int k_Capacity = 6;
        public override int Capacity => k_Capacity;

        private RenderPassTexture m_Input0;
        private RenderPassTexture m_Input1;
        private RenderPassTexture m_Input2;
        private RenderPassTexture m_Input3;
        private RenderPassTexture m_Input4;
        private RenderPassTexture m_Input5;

        protected override void SetInput(int index, RenderPassTexture input)
        {
            switch (index)
            {
                case 0: m_Input0 = input; break;
                case 1: m_Input1 = input; break;
                case 2: m_Input2 = input; break;
                case 3: m_Input3 = input; break;
                case 4: m_Input4 = input; break;
                case 5: m_Input5 = input; break;
                default: throw new IndexOutOfRangeException();
            }
        }

        public override RenderPassTexture GetInput(int index)
        {
            switch (index)
            {
                case 0: return m_Input0;
                case 1: return m_Input1;
                case 2: return m_Input2;
                case 3: return m_Input3;
                case 4: return m_Input4;
                case 5: return m_Input5;
                default: throw new IndexOutOfRangeException();
            }
        }
    }
}