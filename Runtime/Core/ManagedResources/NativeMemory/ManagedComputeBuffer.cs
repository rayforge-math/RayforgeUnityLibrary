using Rayforge.ManagedResources.Abstractions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace Rayforge.ManagedResources.NativeMemory
{
    /// <summary>
    /// Managed wrapper around Unity's <see cref="ComputeBuffer"/> that handles allocation, data upload, and cleanup.
    /// Inherits from <see cref="ManagedBuffer{TDesc,TBuffer}"/> for generic GPU resource management.
    /// </summary>
    public sealed class ManagedComputeBuffer : ManagedBuffer<ComputeBufferDescriptor, ComputeBuffer>
    {
        /// <summary>
        /// Allocates a new compute buffer based on the given descriptor.
        /// </summary>
        public ManagedComputeBuffer(ComputeBufferDescriptor desc)
            : base(new ComputeBuffer(desc.count, desc.stride, desc.type), desc)
        { }

        /// <summary>
        /// Creates a compute buffer.
        /// Automatically determines stride based on <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The element type stored in the compute buffer.</typeparam>
        /// <param name="count">Number of elements in the buffer.</param>
        /// <param name="type">Optional compute buffer type. Default is structured.</param>
        /// <returns>A leased buffer representing the rented <see cref="ManagedComputeBuffer"/>.</returns>
        public static ManagedComputeBuffer Create<T>(int count, ComputeBufferType type = ComputeBufferType.Structured)
            where T : unmanaged
        {
            int stride = Marshal.SizeOf<T>();
            var desc = new ComputeBufferDescriptor { count = count, stride = stride, type = type };
            return new ManagedComputeBuffer(desc);
        }

        /// <summary>
        /// Uploads raw array data to the GPU buffer.
        /// </summary>
        public void SetData(Array data)
            => m_Buffer.SetData(data);

        /// <summary>
        /// Uploads a strongly-typed list to the GPU buffer.
        /// </summary>
        public void SetData<T>(List<T> data) where T : struct
            => m_Buffer.SetData(data);

        /// <summary>
        /// Uploads a native array (e.g., NativeArray) to the GPU buffer.
        /// </summary>
        public void SetData<T>(NativeArray<T> data) where T : struct
            => m_Buffer.SetData(data);

        /// <summary>
        /// Reads back data from the GPU buffer into a CPU array.
        /// </summary>
        public void GetData(Array data)
            => m_Buffer.GetData(data);

        /// <summary>
        /// Sets the internal counter value for Append/Consume buffers.
        /// </summary>
        public void SetCounterValue(uint counterValue)
            => m_Buffer.SetCounterValue(counterValue);

        /// <summary>
        /// Uploads data from a <see cref="IComputeData{T}"/> container to the buffer.
        /// Useful for e.g. constant buffers.
        /// </summary>
        public void SetData<T>(IComputeData<T> data)
            where T : unmanaged
        {
            if (data == null) return;
            SetData(new T[] { data.RawData });
        }

        /// <summary>
        /// Uploads data from a <see cref="IComputeDataArray{T}"/> container to the buffer.
        /// Useful for uniform-style data structures.
        /// </summary>
        public void SetData<T>(IComputeDataArray<T> data)
            where T : unmanaged
        {
            if (data == null) return;
            SetData(data.RawData);
        }

        /// <summary>
        /// Releases the GPU buffer.
        /// After calling this, the buffer is no longer valid and internal references are cleared.
        /// </summary>
        public override void Release()
        {
            if (m_Buffer != null)
            {
                m_Buffer.Release();
                m_Buffer = null;
            }
        }

        /// <summary>
        /// Compares buffer instances by reference.
        /// For pooled or managed buffers, reference equality is usually sufficient.
        /// </summary>
        public override bool Equals(ManagedBuffer<ComputeBufferDescriptor, ComputeBuffer> other)
            => ReferenceEquals(this, other);
    }
}