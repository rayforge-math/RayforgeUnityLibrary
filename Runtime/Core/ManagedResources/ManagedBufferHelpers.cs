using UnityEngine;

namespace Rayforge.ManagedResources.NativeMemory.Helpers
{
    /// <summary>
    /// Provides extension methods for binding <see cref="ComputeBuffer"/> objects as constant buffers
    /// to materials and material property blocks.
    /// </summary>
    public static class MaterialCBufferExtensions
    {
        /// <summary>
        /// Binds a <see cref="ManagedComputeBuffer"/> as a constant buffer to a <see cref="MaterialPropertyBlock"/>.
        /// </summary>
        /// <param name="mpb">The <see cref="MaterialPropertyBlock"/> to update.</param>
        /// <param name="nameId">The shader property ID of the constant buffer to set.</param>
        /// <param name="buffer">The managed compute buffer containing the constant data.</param>
        /// <param name="offset">The starting offset within the buffer in bytes. Defaults to <c>0</c>.</param>
        public static void SetCBuffer(this MaterialPropertyBlock mpb, int nameId, ManagedComputeBuffer buffer, int offset = 0)
        {
            mpb.SetConstantBuffer(nameId, buffer.Buffer, offset, buffer.Buffer.stride);
        }

        /// <summary>
        /// Binds a <see cref="ManagedComputeBuffer"/> as a constant buffer directly to a <see cref="Material"/>.
        /// </summary>
        /// <param name="material">The target material to update.</param>
        /// <param name="nameId">The shader property ID of the constant buffer to set.</param>
        /// <param name="buffer">The managed compute buffer containing the constant data.</param>
        /// <param name="offset">The starting offset within the buffer in bytes. Defaults to <c>0</c>.</param>
        public static void SetCBuffer(this Material material, int nameId, ManagedComputeBuffer buffer, int offset = 0)
        {
            material.SetConstantBuffer(nameId, buffer.Buffer, offset, buffer.Buffer.stride);
        }

        /// <summary>
        /// Binds a <see cref="ManagedComputeBuffer"/> as a constant buffer directly to a <see cref="ComputeShader"/>.
        /// </summary>
        /// <param name="shader">The target shader to update.</param>
        /// <param name="nameId">The shader property ID of the constant buffer to set.</param>
        /// <param name="buffer">The managed compute buffer containing the constant data.</param>
        /// <param name="offset">The starting offset within the buffer in bytes. Defaults to <c>0</c>.</param>
        public static void SetCBuffer(this ComputeShader shader, int nameId, ManagedComputeBuffer buffer, int offset = 0)
        {
            shader.SetConstantBuffer(nameId, buffer.Buffer, offset, buffer.Buffer.stride);
        }
    }
}