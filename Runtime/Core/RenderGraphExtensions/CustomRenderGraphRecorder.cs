using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.RenderGraphExtensions.RenderPasses
{
    /// <summary>
    /// Provides shader property identifiers for common blit parameters used when drawing
    /// full-screen procedural triangles inside RenderGraph passes.
    /// </summary>
    public static class BlitParameters
    {
        private const string k_BlitTextureName = "_BlitTexture";
        /// <summary>Shader property name used to bind the source texture for a blit pass.</summary>
        public static string BlitTextureName => k_BlitTextureName;
        private const string k_BlitScaleBiasName = "_BlitScaleBias";
        /// <summary>Shader property name used to provide scale and bias for sampling operations.</summary>
        public static string BlitScaleBiasName => k_BlitScaleBiasName;

        private static readonly int k_BlitTextureId = Shader.PropertyToID(k_BlitTextureName);
        /// <summary>Property ID for the blit texture.</summary>
        public static int BlitTextureId => k_BlitTextureId;
        private static readonly int k_BlitScaleBiasId = Shader.PropertyToID(k_BlitScaleBiasName);
        /// <summary>Property ID for the blit scale/bias vector.</summary>
        public static int BlitScaleBiasId => k_BlitScaleBiasId;
    }

    /// <summary>
    /// Represents a texture input bound to a pass, associating a shader property slot
    /// with a RenderGraph texture handle.
    /// </summary>
    public struct InputTexture
    {
        public int propertyId;
        public TextureHandle handle;
    }

    /// <summary>
    /// Base class for RenderGraph pass input/output configuration and material binding.
    /// </summary>
    public abstract class CustomPassData : IDisposable
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
        public virtual void Copy(CustomPassData other)
        {
            m_Destination = other.m_Destination;
            m_Material = other.m_Material;
            m_PropertyBlock = other.m_PropertyBlock;
            m_PassId = other.m_PassId;
        }

        /// <summary>
        /// Enumerates all valid texture inputs used by this pass.
        /// </summary>
        public abstract IEnumerable<InputTexture> PassInput { get; }
    }

    /// <summary>
    /// Pass data container for passes that consume exactly one texture input.
    /// </summary>
    public class SingleInputPassData : CustomPassData
    {
        private InputTexture m_Input;

        /// <summary>Returns the configured input texture.</summary>
        public InputTexture Input => m_Input;

        /// <summary>Assigns the input texture using the default blit texture property.</summary>
        public void SetInput(TextureHandle handle)
            => SetInput(new InputTexture { handle = handle, propertyId = BlitParameters.BlitTextureId });

        /// <summary>Assigns the input texture using a custom shader property ID.</summary>
        public void SetInput(int propertyId, TextureHandle handle)
            => SetInput(new InputTexture { handle = handle, propertyId = propertyId });

        /// <summary>Assigns the input texture.</summary>
        public void SetInput(InputTexture input)
            => m_Input = input;

        /// <inheritdoc/>
        public override IEnumerable<InputTexture> PassInput
        {
            get { yield return m_Input; }
        }
    }


    /// <summary>
    /// Base class for passes supporting multiple texture inputs.
    /// </summary>
    public abstract class MultiInputPassData : CustomPassData
    {
        /// <summary>Maximum number of supported input textures.</summary>
        public abstract int Capacity { get; }

        /// <summary>Assigns an input texture to the specified index.</summary>
        protected abstract void SetInput(int index, InputTexture input);

        /// <summary>Assigns an input texture to the specified index.</summary>
        public void SetInput(int index, int propertyId, TextureHandle handle)
            => SetInput(index, new InputTexture { propertyId = propertyId, handle = handle });

        /// <summary>Returns the input texture stored at the given index.</summary>
        public abstract InputTexture GetInput(int index);

        /// <inheritdoc/>
        /// <remarks>
        /// We avoid using an array here because Unity retrieves PassData objects from an internal pool.
        /// Creating or copying an array would immediately trigger heap allocations and garbage collection,
        /// which we want to avoid during rendering.
        /// </remarks>
        public override IEnumerable<InputTexture> PassInput
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

        private InputTexture m_Input0;
        private InputTexture m_Input1;

        protected override void SetInput(int index, InputTexture input)
        {
            switch (index)
            {
                case 0: m_Input0 = input; break;
                case 1: m_Input1 = input; break;
                default: throw new IndexOutOfRangeException();
            }
        }

        public override InputTexture GetInput(int index)
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

        private InputTexture m_Input0;
        private InputTexture m_Input1;
        private InputTexture m_Input2;

        protected override void SetInput(int index, InputTexture input)
        {
            switch (index)
            {
                case 0: m_Input0 = input; break;
                case 1: m_Input1 = input; break;
                case 2: m_Input2 = input; break;
                default: throw new IndexOutOfRangeException();
            }
        }

        public override InputTexture GetInput(int index)
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

        private InputTexture m_Input0;
        private InputTexture m_Input1;
        private InputTexture m_Input2;
        private InputTexture m_Input3;

        protected override void SetInput(int index, InputTexture input)
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

        public override InputTexture GetInput(int index)
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

    /// <summary>
    /// Provides helper functions for adding procedural full-screen blit passes
    /// to the RenderGraph API.
    /// </summary>
    public static class CustomRecorder
    {
        private static MaterialPropertyBlock s_PropertyBlock = new();

        /// <summary>
        /// Delegate used to set up pass data before execution.
        /// </summary>
        public delegate void PassDataUpdate<TpassData>(TpassData passData) where TpassData : CustomPassData;
        /// <summary>
        /// Delegate used to configure additional rendering parameters during pass execution.
        /// </summary>
        public delegate void RenderDataUpdate<Tcmd>(Tcmd cmd, MaterialPropertyBlock mpb) where Tcmd : BaseCommandBuffer;

        /// <summary>
        /// Adds a custom RenderGraph pass executed using <see cref="UnsafeCommandBuffer"/>.
        /// This is useful for low-level full-screen operations, manual blits or passes that require
        /// direct command buffer access.
        /// </summary>
        /// <typeparam name="TpassData">Pass data type used to configure inputs, outputs and material state.</typeparam>
        /// <param name="renderGraph">RenderGraph instance the pass is added to.</param>
        /// <param name="passName">Name used for debugging and RenderGraph visualization.</param>
        /// <param name="passDataUpdate">
        /// Delegate used to configure the pass input/output state before execution.  
        /// Called when the pass is created.
        /// </param>
        /// <param name="renderDataUpdate">
        /// Optional delegate invoked during execution, allowing additional command buffer setup
        /// before drawing (e.g., keyword setup, material property block adjustments).
        /// </param>
        public static void AddUnsafeRenderPass<TpassData>(RenderGraph renderGraph, string passName, PassDataUpdate<TpassData> passDataUpdate, RenderDataUpdate<UnsafeCommandBuffer> renderDataUpdate = null)
            where TpassData : CustomPassData, new()
        {
            using (var builder = renderGraph.AddUnsafePass(passName, out TpassData data))
            {
                passDataUpdate.Invoke(data);

                foreach (var input in data.PassInput)
                {
                    builder.UseTexture(input.handle, AccessFlags.Read);
                }
                builder.UseTexture(data.Destination, AccessFlags.Write);

                builder.SetRenderFunc((TpassData data, UnsafeGraphContext ctx) =>
                {
                    MaterialPropertyBlock propertyBlock = data.PropertyBlock;
                    if (propertyBlock == null)
                    {
                        s_PropertyBlock.Clear();
                        propertyBlock = s_PropertyBlock;
                    }

                    renderDataUpdate?.Invoke(ctx.cmd, propertyBlock);

                    CommandBuffer unsafeCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    unsafeCmd.SetRenderTarget(data.Destination, 0, CubemapFace.Unknown, 0);

                    foreach (var input in data.PassInput)
                    {
                        propertyBlock.SetTexture(input.propertyId, input.handle);
                    }
                    propertyBlock.SetVector(BlitParameters.BlitScaleBiasId, Vector2.one);

                    unsafeCmd.DrawProcedural(Matrix4x4.identity, data.Material, data.PassId, MeshTopology.Triangles, 3, 1, propertyBlock);
                });
            }
        }

        /// <summary>
        /// Adds a standard raster RenderGraph pass. Render target attachments are handled automatically.
        /// Recommended for most full-screen rendering operations unless low-level native buffer control is required.
        /// </summary>
        /// <typeparam name="TpassData">Pass data type used to configure material and input/output state.</typeparam>
        /// <param name="renderGraph">RenderGraph instance to add the pass to.</param>
        /// <param name="passName">Display name used in the RenderGraph debug view.</param>
        /// <param name="passDataUpdate">
        /// Delegate called when the pass is created, used to configure input textures and target output.
        /// </param>
        /// <param name="renderDataUpdate">
        /// Optional delegate invoked during pass execution to customize material property state.
        /// </param>
        public static void AddRasterRenderPass<TpassData>(RenderGraph renderGraph, string passName, PassDataUpdate<TpassData> passDataUpdate, RenderDataUpdate<RasterCommandBuffer> renderDataUpdate = null)
            where TpassData : CustomPassData, new()
        {
            using (var builder = renderGraph.AddRasterRenderPass(passName, out TpassData data))
            {
                passDataUpdate.Invoke(data);

                foreach (var input in data.PassInput)
                {
                    builder.UseTexture(input.handle, AccessFlags.Read);
                }
                builder.SetRenderAttachment(data.Destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((TpassData data, RasterGraphContext ctx) =>
                {
                    MaterialPropertyBlock propertyBlock = data.PropertyBlock;
                    if (propertyBlock == null)
                    {
                        s_PropertyBlock.Clear();
                        propertyBlock = s_PropertyBlock;
                    }

                    renderDataUpdate?.Invoke(ctx.cmd, propertyBlock);

                    foreach (var input in data.PassInput)
                    {
                        propertyBlock.SetTexture(input.propertyId, input.handle);
                    }
                    propertyBlock.SetVector(BlitParameters.BlitScaleBiasId, Vector2.one);

                    ctx.cmd.DrawProcedural(Matrix4x4.identity, data.Material, data.PassId, MeshTopology.Triangles, 3, 1, propertyBlock);
                });
            }
        }
    }
}