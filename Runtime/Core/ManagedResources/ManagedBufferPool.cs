using Rayforge.ManagedResources.NativeMemory;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace Rayforge.ManagedResources.Pooling
{
    /// <summary>
    /// Delegate invoked when a leased buffer is returned to the pool.
    /// The delegate should handle marking the buffer as free and any custom logic.
    /// Returns true if the buffer was successfully returned; false if the buffer was not recognized or could not be returned.
    /// </summary>
    /// <typeparam name="Tdesc">The descriptor type of the buffer.</typeparam>
    /// <typeparam name="Tbuffer">The managed buffer type.</typeparam>
    /// <param name="buffer">The buffer being returned to the pool.</param>
    /// <returns>True if the buffer was successfully returned; otherwise, false.</returns>
    public delegate bool LeasedReturnFunc<Tdesc, Tbuffer>(Tbuffer buffer)
        where Tbuffer : IPooledBuffer<Tdesc>
        where Tdesc : unmanaged, IEquatable<Tdesc>;

    /// <summary>
    /// Base class for all leased buffer wrappers. Handles lifetime management,
    /// validation, and return-to-pool semantics.
    /// </summary>
    public abstract class LeasedBufferBase<Tdesc, Tbuffer>
        where Tbuffer : IPooledBuffer<Tdesc>
        where Tdesc : unmanaged, IEquatable<Tdesc>
    {
        protected Tbuffer m_BufferHandle;
        protected bool m_Valid;
        private readonly LeasedReturnFunc<Tdesc, Tbuffer> m_OnReturn;

        /// <summary>
        /// The underlying pooled buffer instance.
        /// Throws if accessed after return.
        /// </summary>
        public Tbuffer BufferHandle
        {
            get
            {
                if (!m_Valid)
                    throw new InvalidOperationException("Cannot access buffer after it has been returned to the pool.");
                return m_BufferHandle;
            }
        }

        /// <summary>
        /// Indicates whether the lease is still active.
        /// </summary>
        public bool IsValid => m_Valid;

        protected LeasedBufferBase(Tbuffer buffer, LeasedReturnFunc<Tdesc, Tbuffer> onReturnHandle)
        {
            m_BufferHandle = buffer ?? throw new ArgumentNullException(nameof(buffer));
            m_OnReturn = onReturnHandle ?? throw new ArgumentNullException(nameof(onReturnHandle));
            m_Valid = true;
        }

        /// <summary>
        /// Returns the buffer to the pool and invalidates this lease.
        /// </summary>
        /// <returns>True if successfully returned; otherwise false.</returns>
        public virtual bool Return()
        {
            if (!m_Valid)
                return false;

            m_Valid = false;
            return m_OnReturn.Invoke(m_BufferHandle);
        }
    }

    /// <summary>
    /// A leased buffer instance returned from a pool. The buffer may be used only
    /// while the lease is valid. Once returned or disposed, further access throws.
    /// </summary>
    /// <typeparam name="Tdesc">
    /// Descriptor type used to define the buffer's configuration.
    /// Must be unmanaged so it can be used efficiently as a lookup key.
    /// </typeparam>
    /// <typeparam name="Tbuffer">
    /// The buffer type associated with <typeparamref name="Tdesc"/> that is
    /// managed by the pool.
    /// </typeparam>
    public class LeasedBuffer<Tdesc, Tbuffer> : LeasedBufferBase<Tdesc, Tbuffer>
        where Tbuffer : IPooledBuffer<Tdesc>
        where Tdesc : unmanaged, IEquatable<Tdesc>
    {
        public LeasedBuffer(Tbuffer buffer, LeasedReturnFunc<Tdesc, Tbuffer> onReturnHandle)
            : base(buffer, onReturnHandle) { }
    }

    /// <summary>
    /// Delegate invoked to check whether a leased buffer still fits within the pool's current batching constraints.
    /// Used by batched pools to determine if a buffer can be reused for a given element count or requires reallocation.
    /// </summary>
    /// <typeparam name="Tdesc">The descriptor type describing the buffer properties.</typeparam>
    /// <param name="desc">The descriptor instance of the buffer to evaluate.</param>
    /// <param name="desiredCount">The desired number of elements to validate against the current batch allocation.</param>
    /// <returns>True if the buffer is still valid for the given batch size; otherwise, false.</returns>
    public delegate bool BatchCheckFunc<Tdesc>(Tdesc desc, int desiredCount)
        where Tdesc : IBatchingDescriptor;

    /// <summary>
    /// Delegate invoked to request a new batched buffer from the pool.
    /// Typically used when the current leased buffer is too small and needs resizing.
    /// </summary>
    /// <typeparam name="Tdesc">The descriptor type describing the buffer properties.</typeparam>
    /// <typeparam name="Tbuffer">The managed buffer type.</typeparam>
    /// <param name="buffer">The underlying buffer to swap.</param>
    /// <param name="desiredCount">The requested element count.</param>
    /// <returns>A new <see cref="BatchedLeasedBuffer{Tdesc,Tbuffer}"/> of the requested batch size.</returns>
    public delegate Tbuffer RequestBatchedBufferFunc<Tdesc, Tbuffer>(Tdesc buffer, int desiredCount)
        where Tdesc : unmanaged, IEquatable<Tdesc>, IBatchingDescriptor;


    /// <summary>
    /// Represents a leased buffer that supports batch validation within a pooled context.
    /// This type extends <see cref="LeasedBuffer{Tdesc,Tbuffer}"/> by providing a mechanism
    /// to verify whether the current buffer still fits within the batching constraints of the pool.
    /// </summary>
    /// <typeparam name="Tdesc">The descriptor type describing the buffer properties.</typeparam>
    /// <typeparam name="Tbuffer">The managed buffer type implementing <see cref="IPooledBuffer{Tdesc}"/>.</typeparam>
    public class BatchedLeasedBuffer<Tdesc, Tbuffer> : LeasedBufferBase<Tdesc, Tbuffer>
        where Tbuffer : IPooledBuffer<Tdesc>
        where Tdesc : unmanaged, IEquatable<Tdesc>, IBatchingDescriptor
    {
        private readonly BatchCheckFunc<Tdesc> m_OnBatchCheck;
        private readonly RequestBatchedBufferFunc<Tdesc, Tbuffer> m_RequestNewBuffer;

        /// <summary>
        /// Creates a new batched leased buffer.
        /// </summary>
        /// <param name="buffer">The underlying managed buffer.</param>
        /// <param name="onReturnHandle">
        /// Delegate invoked when the buffer is returned to the pool.
        /// </param>
        /// <param name="onBatchCheckHandle">
        /// Delegate invoked to check if the buffer still fits the pool's batching constraints for a given element count.
        /// </param>
        /// <param name="requestNewBufferFunc">
        /// Delegate invoked to request a new buffer of appropriate batch size if the current buffer is too small.
        /// </param>
        public BatchedLeasedBuffer(
            Tbuffer buffer,
            LeasedReturnFunc<Tdesc, Tbuffer> onReturnHandle,
            BatchCheckFunc<Tdesc> onBatchCheckHandle,
            RequestBatchedBufferFunc<Tdesc, Tbuffer> requestNewBufferFunc)
            : base(buffer, onReturnHandle)
        {
            m_OnBatchCheck = onBatchCheckHandle ?? throw new ArgumentNullException(nameof(onBatchCheckHandle));
            m_RequestNewBuffer = requestNewBufferFunc ?? throw new ArgumentNullException(nameof(requestNewBufferFunc));
        }

        /// <summary>
        /// Checks whether the current buffer still fits within the batch size constraints.
        /// </summary>
        /// <param name="desiredCount">The desired element count to validate.</param>
        /// <returns>
        /// <c>true</c> if the buffer is still valid for the given batch size; <c>false</c> if a resize is needed.
        /// </returns>
        public bool EnsureBatchSize(int desiredCount)
            => m_OnBatchCheck?.Invoke(BufferHandle.Descriptor, desiredCount) ?? false;

        /// <summary>
        /// Replaces the current buffer with a new buffer of the requested batch size.
        /// The current buffer is returned to the pool before replacement.
        /// </summary>
        /// <param name="desiredCount">The desired element count for the new buffer.</param>
        /// <remarks>
        /// After calling this method, <see cref="BufferHandle"/> points to the newly acquired buffer.
        /// </remarks>
        public void Resize(int desiredCount)
        {
            var desc = BufferHandle.Descriptor;
            Return();
            m_BufferHandle = m_RequestNewBuffer.Invoke(desc, desiredCount);
        }
    }

    /// <summary>
    /// Factory to create a new buffer from a descriptor.
    /// </summary>
    public delegate Tbuffer BufferCreateFunc<Tdesc, Tbuffer>(Tdesc desc)
        where Tbuffer : IPooledBuffer<Tdesc>
        where Tdesc : unmanaged, IEquatable<Tdesc>;

    /// <summary>
    /// Callback invoked when a buffer is permanently released from the pool.
    /// </summary>
    public delegate void BufferReleaseFunc<Tdesc, Tbuffer>(Tbuffer buffer)
        where Tbuffer : IPooledBuffer<Tdesc>
        where Tdesc : unmanaged, IEquatable<Tdesc>;

    /// <summary>
    /// Base class for buffer pools that manage reusable buffer objects and hand them out as lease wrappers.
    /// This pool is *not* thread-safe; derived classes must implement any required thread-safety, eviction policy, 
    /// or automatic cleanup behavior.
    ///
    /// Provides mechanisms for:
    /// - Renting a buffer as a lease object of type <typeparamref name="Tlease"/>.
    /// - Returning buffers to the pool.
    /// - Creating new buffers directly without leasing (useful for batch resizing or hot-swapping).
    /// </summary>
    /// <typeparam name="Tdesc">Descriptor type used to categorize buffers. Must be unmanaged and implement <see cref="IEquatable{Tdesc}"/>.</typeparam>
    /// <typeparam name="Tbuffer">The pooled buffer type, must implement <see cref="IPooledBuffer{Tdesc}"/>.</typeparam>
    /// <typeparam name="Tlease">The lease wrapper type returned by the pool, must inherit from <see cref="LeasedBufferBase{Tdesc,Tbuffer}"/>.</typeparam>
    public abstract class LeasedBufferPoolBase<Tdesc, Tbuffer, Tlease> : IDisposable
        where Tbuffer : IPooledBuffer<Tdesc>
        where Tdesc : unmanaged, IEquatable<Tdesc>
        where Tlease : LeasedBufferBase<Tdesc, Tbuffer>
    {
        /// <summary>
        /// Factory used to create new buffers on demand.
        /// </summary>
        protected readonly BufferCreateFunc<Tdesc, Tbuffer> m_CreateFunc;

        /// <summary>
        /// Factory used to permanently release a buffer.
        /// </summary>
        protected readonly BufferReleaseFunc<Tdesc, Tbuffer> m_ReleaseFunc;

        /// <summary>
        /// Free buffers grouped by descriptor for quick reuse.
        /// </summary>
        protected readonly Dictionary<Tdesc, Stack<Tbuffer>> m_FreeDict = new();

        /// <summary>
        /// Buffers currently leased out to consumers.
        /// </summary>
        protected readonly HashSet<Tbuffer> m_Reserved = new();

        /// <summary>
        /// Constructs a new base buffer pool with the provided create and release functions.
        /// </summary>
        /// <param name="createFunc">Function used to create a new buffer when the pool has no free buffers.</param>
        /// <param name="releaseFunc">Function used to permanently release a buffer when the pool is cleared or disposed.</param>
        protected LeasedBufferPoolBase(BufferCreateFunc<Tdesc, Tbuffer> createFunc, BufferReleaseFunc<Tdesc, Tbuffer> releaseFunc)
        {
            m_CreateFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            m_ReleaseFunc = releaseFunc ?? throw new ArgumentNullException(nameof(releaseFunc));
        }

        /// <summary>
        /// Derived pools must implement how the lease wrapper is constructed from a raw buffer.
        /// This method is used internally by <see cref="Rent"/> to wrap a pooled buffer in a lease object of type <typeparamref name="Tlease"/>.
        /// </summary>
        /// <param name="buffer">The raw buffer to wrap in a lease.</param>
        /// <returns>A lease object of type <typeparamref name="Tlease"/> that wraps the given buffer.</returns>
        protected abstract Tlease CreateLease(Tbuffer buffer);

        /// <summary>
        /// Rents a buffer internally from the pool. If no free buffer exists, a new one is created.
        /// This method returns the raw buffer without wrapping it in a lease.
        /// </summary>
        /// <param name="desc">Descriptor for the buffer to rent.</param>
        /// <returns>The rented buffer instance of type <typeparamref name="Tbuffer"/>.</returns>
        protected virtual Tbuffer RentInternal(Tdesc desc)
        {
            Tbuffer buffer;

            if (m_FreeDict.TryGetValue(desc, out var stack) && stack.Count > 0)
            {
                buffer = stack.Pop();
            }
            else
            {
                buffer = m_CreateFunc.Invoke(desc);
            }

            m_Reserved.Add(buffer);
            return buffer;
        }

        /// <summary>
        /// Rents a buffer from the pool. If no free buffer exists, a new one is created.
        /// The buffer is wrapped in a lease object of type <typeparamref name="Tlease"/> via <see cref="CreateLease"/>.
        /// </summary>
        /// <param name="desc">Descriptor used to identify or create the buffer.</param>
        /// <returns>A lease object wrapping the rented buffer.</returns>
        public virtual Tlease Rent(Tdesc desc)
        {
            var buffer = RentInternal(desc);
            return CreateLease(buffer);
        }

        /// <summary>
        /// Called by a lease when its buffer is returned to the pool.
        /// Adds the buffer back into the free collection for reuse.
        /// </summary>
        /// <param name="buffer">The buffer being returned.</param>
        /// <returns>True if the buffer was successfully returned; false if it was not recognized as reserved.</returns>
        protected virtual bool Return(Tbuffer buffer)
        {
            if (!m_Reserved.Remove(buffer))
                return false;

            if (!m_FreeDict.TryGetValue(buffer.Descriptor, out var stack))
            {
                stack = new Stack<Tbuffer>();
                m_FreeDict[buffer.Descriptor] = stack;
            }

            stack.Push(buffer);
            return true;
        }

        /// <summary>
        /// Releases all buffers in both free and reserved collections.
        /// After this call, the pool is empty and cannot be reused until new buffers are created.
        /// </summary>
        public virtual void Dispose()
        {
            foreach (var stack in m_FreeDict.Values)
                foreach (var buffer in stack)
                    m_ReleaseFunc.Invoke(buffer);

            foreach (var buffer in m_Reserved)
                m_ReleaseFunc.Invoke(buffer);

            m_FreeDict.Clear();
            m_Reserved.Clear();
        }

        /// <summary>
        /// Permanently releases all unused buffers without affecting those currently leased.
        /// This allows clearing memory pressure while keeping active leases valid.
        /// </summary>
        public virtual void ClearUnused()
        {
            foreach (var stack in m_FreeDict.Values)
                foreach (var buffer in stack)
                    m_ReleaseFunc.Invoke(buffer);

            m_FreeDict.Clear();
        }
    }

    /// <summary>
    /// Simple buffer pool that returns standard leased buffers.
    /// Wraps buffers in <see cref="LeasedBuffer{Tdesc, Tbuffer}"/> when rented.
    /// </summary>
    /// <typeparam name="Tdesc">Descriptor type for buffer configuration. Must be unmanaged and implement <see cref="IEquatable{Tdesc}"/>.</typeparam>
    /// <typeparam name="Tbuffer">Type of buffer managed by the pool. Must implement <see cref="IPooledBuffer{Tdesc}"/>.</typeparam>
    public partial class LeasedBufferPool<Tdesc, Tbuffer> : LeasedBufferPoolBase<Tdesc, Tbuffer, LeasedBuffer<Tdesc, Tbuffer>>
        where Tbuffer : IPooledBuffer<Tdesc>
        where Tdesc : unmanaged, IEquatable<Tdesc>
    {
        /// <summary>
        /// Creates a new leased buffer pool.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer when the pool is empty.</param>
        /// <param name="releaseFunc">Function to permanently release a buffer.</param>
        public LeasedBufferPool(BufferCreateFunc<Tdesc, Tbuffer> createFunc, BufferReleaseFunc<Tdesc, Tbuffer> releaseFunc)
            : base(createFunc, releaseFunc) { }

        /// <summary>
        /// Wraps a raw buffer in a leased buffer that automatically returns to the pool on disposal.
        /// </summary>
        /// <param name="buffer">The raw buffer to wrap.</param>
        /// <returns>A <see cref="LeasedBuffer{Tdesc, Tbuffer}"/> representing the leased buffer.</returns>
        protected override LeasedBuffer<Tdesc, Tbuffer> CreateLease(Tbuffer buffer)
            => new LeasedBuffer<Tdesc, Tbuffer>(buffer, Return);
    }

    /// <summary>
    /// A specialized buffer pool that supports batching for sequential buffers.
    /// Useful for ComputeBuffers or NativeArrays where allocations can be rounded
    /// to a batch size to reduce frequent reallocations.
    /// Wraps buffers in <see cref="BatchedLeasedBuffer{Tdesc, Tbuffer}"/> when rented.
    /// </summary>
    /// <typeparam name="Tdesc">Descriptor type describing the buffer. Must be unmanaged, implement <see cref="IEquatable{Tdesc}"/>, and <see cref="IBatchingDescriptor"/>.</typeparam>
    /// <typeparam name="Tbuffer">Type of the managed buffer (e.g., ManagedComputeBuffer). Must implement <see cref="IPooledBuffer{Tdesc}"/>.</typeparam>
    public partial class BatchedLeasedBufferPool<Tdesc, Tbuffer> : LeasedBufferPoolBase<Tdesc, Tbuffer, BatchedLeasedBuffer<Tdesc, Tbuffer>>
        where Tbuffer : IPooledBuffer<Tdesc>
        where Tdesc : unmanaged, IEquatable<Tdesc>, IBatchingDescriptor
    {
        /// <summary>
        /// Minimum allocation size to ensure a base buffer size.
        /// </summary>
        public int BaseSize { get; }

        /// <summary>
        /// Batch size for rounding allocations. If 0, batching is disabled.
        /// </summary>
        public int BatchSize { get; }

        /// <summary>
        /// Constructs a new batched buffer pool.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer when the pool is empty.</param>
        /// <param name="releaseFunc">Function to release a buffer permanently.</param>
        /// <param name="baseSize">Minimum base allocation size.</param>
        /// <param name="batchSize">Batch size for rounding allocations.</param>
        public BatchedLeasedBufferPool(
            BufferCreateFunc<Tdesc, Tbuffer> createFunc,
            BufferReleaseFunc<Tdesc, Tbuffer> releaseFunc,
            int baseSize = 1,
            int batchSize = 0)
            : base(createFunc, releaseFunc)
        {
            BaseSize = Math.Max(baseSize, 1);
            BatchSize = Math.Max(batchSize, 0);
        }

        /// <summary>
        /// Wraps a raw buffer in a leased buffer that automatically returns to the pool.
        /// </summary>
        /// <param name="buffer">The raw buffer to wrap.</param>
        /// <returns>A <see cref="BatchedLeasedBuffer{Tdesc, Tbuffer}"/> representing the leased buffer.</returns>
        protected override BatchedLeasedBuffer<Tdesc, Tbuffer> CreateLease(Tbuffer buffer)
            => new BatchedLeasedBuffer<Tdesc, Tbuffer>(
                buffer,
                Return,
                (desc, count) => BatchedCount(count) == desc.Count,
                (desc, count) => RentInternal(desc)
            );

        /// <summary>
        /// Computes the adjusted count based on batching rules.
        /// Rounds up to the nearest multiple of <see cref="BatchSize"/> if batching is enabled.
        /// </summary>
        /// <param name="requestedCount">The requested element count.</param>
        /// <returns>The adjusted element count according to batch settings.</returns>
        private int BatchedCount(int requestedCount)
        {
            int adjusted = Math.Max(requestedCount, BaseSize);
            if (BatchSize > 0)
                adjusted = ((adjusted + BatchSize - 1) / BatchSize) * BatchSize;
            return adjusted;
        }

        /// <summary>
        /// Rents a buffer internally from the pool using batch-adjusted sizing.
        /// This returns the raw buffer without wrapping it in a lease.
        /// </summary>
        /// <param name="desc">Descriptor for the buffer to rent.</param>
        /// <returns>The rented buffer instance of type <typeparamref name="Tbuffer"/>.</returns>
        protected override Tbuffer RentInternal(Tdesc desc)
        {
            desc.Count = BatchedCount(desc.Count);
            return base.RentInternal(desc);
        }

        /// <summary>
        /// Rents a buffer from the pool using batch-adjusted sizing.
        /// The buffer is wrapped in a lease of type <see cref="BatchedLeasedBuffer{Tdesc, Tbuffer}"/>.
        /// </summary>
        /// <param name="desc">Descriptor used to identify or create the buffer.</param>
        /// <returns>A leased buffer of type <see cref="BatchedLeasedBuffer{Tdesc, Tbuffer}"/>.</returns>
        public override BatchedLeasedBuffer<Tdesc, Tbuffer> Rent(Tdesc desc)
        {
            desc.Count = BatchedCount(desc.Count);
            return base.Rent(desc);
        }
    }

    /// <summary>
    /// Specialized managed compute buffer pool with batching support.
    /// Provides default create/destroy logic for <see cref="ManagedComputeBuffer"/> instances.
    /// Wraps buffers in <see cref="BatchedLeasedBuffer{Tdesc, Tbuffer}"/> when rented.
    /// </summary>
    public sealed class ManagedComputeBufferPool : BatchedLeasedBufferPool<ComputeBufferDescriptor, ManagedComputeBuffer>
    {
        /// <summary>
        /// Default constructor using the standard factory functions for creating and releasing buffers.
        /// Initializes the pool with optional base size and batch size for batching behavior.
        /// </summary>
        /// <param name="baseSize">Minimum allocation size (default is 1).</param>
        /// <param name="batchSize">Batch size for rounding allocations (0 disables batching, default is 0).</param>
        public ManagedComputeBufferPool(int baseSize = 1, int batchSize = 0)
            : base(
                createFunc: desc => new ManagedComputeBuffer(desc),
                releaseFunc: buffer => buffer.Dispose(),
                baseSize: baseSize,
                batchSize: batchSize)
        { }

        /// <summary>
        /// Constructor allowing custom create and release functions.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer when the pool is empty.</param>
        /// <param name="releaseFunc">Function to release a buffer permanently.</param>
        /// <param name="baseSize">Minimum allocation size for batching.</param>
        /// <param name="batchSize">Batch size for rounding allocations.</param>
        public ManagedComputeBufferPool(
            BufferCreateFunc<ComputeBufferDescriptor, ManagedComputeBuffer> createFunc,
            BufferReleaseFunc<ComputeBufferDescriptor, ManagedComputeBuffer> releaseFunc,
            int baseSize = 1,
            int batchSize = 0)
            : base(createFunc, releaseFunc, baseSize, batchSize)
        { }
    }

    /// <summary>
    /// Managed pool for system buffers (<see cref="NativeArray{T}"/>) with optional batching.
    /// Provides default create/release functions for <see cref="ManagedSystemBuffer{T}"/>.
    /// </summary>
    /// <typeparam name="T">The struct type stored in the system buffer.</typeparam>
    public sealed class ManagedSystemBufferPool<T> : BatchedLeasedBufferPool<SystemBufferDescriptor, ManagedSystemBuffer<T>>
        where T : unmanaged
    {
        /// <summary>
        /// Default constructor using standard factory methods for system buffers.
        /// </summary>
        /// <param name="baseSize">Minimum allocation size (default is 1).</param>
        /// <param name="batchSize">Batch size for rounding allocations (0 disables batching, default is 0).</param>
        public ManagedSystemBufferPool(int baseSize = 1, int batchSize = 0)
            : base(
                createFunc: desc => new ManagedSystemBuffer<T>(desc),
                releaseFunc: buffer => buffer.Release(),
                baseSize: baseSize,
                batchSize: batchSize)
        { }

        /// <summary>
        /// Constructor allowing custom create and release functions.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer.</param>
        /// <param name="releaseFunc">Function to release a buffer permanently.</param>
        /// <param name="baseSize">Minimum allocation size.</param>
        /// <param name="batchSize">Batch size for rounding allocations.</param>
        public ManagedSystemBufferPool(
            BufferCreateFunc<SystemBufferDescriptor, ManagedSystemBuffer<T>> createFunc,
            BufferReleaseFunc<SystemBufferDescriptor, ManagedSystemBuffer<T>> releaseFunc,
            int baseSize = 1,
            int batchSize = 0)
            : base(createFunc, releaseFunc, baseSize, batchSize)
        { }
    }

    /// <summary>
    /// Managed pool for <see cref="ManagedRenderTexture"/> objects.
    /// Provides default create/release functions.
    /// </summary>
    public sealed class ManagedRenderTexturePool : LeasedBufferPool<RenderTextureDescriptorWrapper, ManagedRenderTexture>
    {
        /// <summary>
        /// Default constructor using standard factory methods for managed render textures.
        /// </summary>
        public ManagedRenderTexturePool()
            : base(
                createFunc: desc => new ManagedRenderTexture(desc, FilterMode.Bilinear, TextureWrapMode.Clamp),
                releaseFunc: buffer => buffer.Release())
        { }

        /// <summary>
        /// Constructor allowing custom create/release functions.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer.</param>
        /// <param name="releaseFunc">Function to release a buffer permanently.</param>
        public ManagedRenderTexturePool(
            BufferCreateFunc<RenderTextureDescriptorWrapper, ManagedRenderTexture> createFunc,
            BufferReleaseFunc<RenderTextureDescriptorWrapper, ManagedRenderTexture> releaseFunc)
            : base(createFunc, releaseFunc)
        { }
    }

    /// <summary>
    /// Managed pool for <see cref="ManagedTexture2D"/> objects.
    /// Provides default create/release functions.
    /// </summary>
    public sealed class ManagedTexture2DPool : LeasedBufferPool<Texture2dDescriptor, ManagedTexture2D>
    {
        /// <summary>
        /// Default constructor using standard factory methods for managed Texture2D objects.
        /// </summary>
        public ManagedTexture2DPool()
            : base(
                createFunc: desc => new ManagedTexture2D(desc),
                releaseFunc: buffer => buffer.Release())
        { }

        /// <summary>
        /// Constructor allowing custom create/release functions.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer.</param>
        /// <param name="releaseFunc">Function to release a buffer permanently.</param>
        public ManagedTexture2DPool(
            BufferCreateFunc<Texture2dDescriptor, ManagedTexture2D> createFunc,
            BufferReleaseFunc<Texture2dDescriptor, ManagedTexture2D> releaseFunc)
            : base(createFunc, releaseFunc)
        { }
    }

    /// <summary>
    /// Managed pool for <see cref="ManagedTexture2DArray"/> objects.
    /// Provides default create/release functions.
    /// </summary>
    public sealed class ManagedTexture2DArrayPool : LeasedBufferPool<Texture2dArrayDescriptor, ManagedTexture2DArray>
    {
        /// <summary>
        /// Default constructor using standard factory methods for managed Texture2DArray objects.
        /// </summary>
        public ManagedTexture2DArrayPool()
            : base(
                createFunc: desc => new ManagedTexture2DArray(desc),
                releaseFunc: buffer => buffer.Release())
        { }

        /// <summary>
        /// Constructor allowing custom create/release functions.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer.</param>
        /// <param name="releaseFunc">Function to release a buffer permanently.</param>
        public ManagedTexture2DArrayPool(
            BufferCreateFunc<Texture2dArrayDescriptor, ManagedTexture2DArray> createFunc,
            BufferReleaseFunc<Texture2dArrayDescriptor, ManagedTexture2DArray> releaseFunc)
            : base(createFunc, releaseFunc)
        { }
    }

    public partial class GlobalManagedPoolBase<Tdesc, Tbuffer>
        where Tbuffer : IPooledBuffer<Tdesc>
        where Tdesc : unmanaged, IEquatable<Tdesc>
    {
        /// <summary>
        /// Internal static pool instance used for the default Rent() method.
        /// </summary>
        protected static LeasedBufferPool<Tdesc, Tbuffer> m_Pool;

        /// <summary>
        /// Rents a buffer from the global pool.
        /// The returned buffer is wrapped in a <see cref="LeasedBuffer{Tdesc,Tbuffer}"/> and automatically returned when disposed.
        /// </summary>
        /// <param name="desc">Descriptor describing the desired compute buffer.</param>
        /// <returns>A leased buffer representing the rented <see cref="ManagedComputeBuffer"/>.</returns>
        public static LeasedBuffer<Tdesc, Tbuffer> Rent(Tdesc desc)
            => m_Pool.Rent(desc);

        /// <summary>
        /// Releases all buffers in the global pool.
        /// After calling this, rented buffers will still work, 
        /// but no old buffers will be reused.
        /// </summary>
        public static void ClearUnused()
            => m_Pool.ClearUnused();

        /// <summary>
        /// Releases all buffers in the global pool.
        /// All buffers will be disposed, no matter the lease state.
        /// For <see cref="Texture2D"/>, the resource stays valid as long as a reference exists.
        /// </summary>
        public static void Dispose()
            => m_Pool.Dispose();
    }

    /// <summary>
    /// Global static pool for <see cref="ManagedComputeBuffer"/> instances.
    /// </summary>
    public sealed class GlobalManagedComputeBufferPool : GlobalManagedPoolBase<ComputeBufferDescriptor, ManagedComputeBuffer>
    {
        /// <summary>
        /// Static constructor initializes the global pool.
        /// </summary>
        static GlobalManagedComputeBufferPool()
        {
            m_Pool = new LeasedBufferPool<ComputeBufferDescriptor, ManagedComputeBuffer>(
                createFunc: (desc) => new ManagedComputeBuffer(desc),
                releaseFunc: (buffer) => buffer.Dispose()
            );
        }

        /// <summary>
        /// Rents a typed compute buffer from the global pool.
        /// Automatically determines stride based on <typeparamref name="T"/>.
        /// The returned buffer is wrapped in a <see cref="LeasedBuffer{Tdesc,Tbuffer}"/> and automatically returned when disposed.
        /// </summary>
        /// <typeparam name="T">The element type stored in the compute buffer.</typeparam>
        /// <param name="count">Number of elements in the buffer.</param>
        /// <param name="type">Optional compute buffer type. Default is structured.</param>
        /// <returns>A leased buffer representing the rented <see cref="ManagedComputeBuffer"/>.</returns>
        public static LeasedBuffer<ComputeBufferDescriptor, ManagedComputeBuffer> Rent<T>(int count, ComputeBufferType type = ComputeBufferType.Structured)
            where T : unmanaged
        {
            int stride = Marshal.SizeOf<T>();
            var desc = new ComputeBufferDescriptor { count = count, stride = stride, type = type };
            return m_Pool.Rent(desc);
        }
    }

    /// <summary>
    /// Global static access to a pool of managed system buffers (<see cref="NativeArray{T}"/>).
    /// Provides simple Rent() for default use,
    /// including a batched variant for sequential buffers to reduce frequent reallocations.
    /// </summary>
    /// <typeparam name="T">The struct type stored in the NativeArray.</typeparam>
    public sealed class GlobalManagedSystemBufferPool<T> : GlobalManagedPoolBase<SystemBufferDescriptor, ManagedSystemBuffer<T>>
        where T : struct
    {
        /// <summary>
        /// Static constructor initializes the default global pool.
        /// </summary>
        static GlobalManagedSystemBufferPool()
        {
            m_Pool = new LeasedBufferPool<SystemBufferDescriptor, ManagedSystemBuffer<T>>(
                createFunc: desc => new ManagedSystemBuffer<T>(desc),
                releaseFunc: buffer => buffer.Release()
            );
        }
    }

    /// <summary>
    /// Global static access to a pool of managed render textures.
    /// Provides simple Rent() for default use.
    /// </summary>
    public sealed class GlobalManagedRenderTexturePool : GlobalManagedPoolBase<RenderTextureDescriptorWrapper, ManagedRenderTexture>
    {
        /// <summary>
        /// Static constructor initializes the default global pool.
        /// </summary>
        static GlobalManagedRenderTexturePool()
        {
            m_Pool = new LeasedBufferPool<RenderTextureDescriptorWrapper, ManagedRenderTexture>(
                createFunc: desc => new ManagedRenderTexture(desc, FilterMode.Bilinear, TextureWrapMode.Clamp),
                releaseFunc: buffer => buffer.Release()
            );
        }
    }

    /// <summary>
    /// Global static access to a pool of managed Texture2D objects.
    /// Provides simple Rent() for default use.
    /// </summary>
    public sealed class GlobalManagedTexture2DPool : GlobalManagedPoolBase<Texture2dDescriptor, ManagedTexture2D>
    {
        /// <summary>
        /// Static constructor initializes the default global pool.
        /// </summary>
        static GlobalManagedTexture2DPool()
        {
            m_Pool = new LeasedBufferPool<Texture2dDescriptor, ManagedTexture2D>(
                createFunc: desc => new ManagedTexture2D(desc),
                releaseFunc: buffer => buffer.Release()
            );
        }
    }

    /// <summary>
    /// Global static access to a pool of managed Texture2DArray objects.
    /// Provides Rent() for default use.
    /// </summary>
    public sealed class GlobalManagedTexture2DArrayPool : GlobalManagedPoolBase<Texture2dArrayDescriptor, ManagedTexture2DArray>
    {
        /// <summary>
        /// Static constructor initializes the default global pool.
        /// </summary>
        static GlobalManagedTexture2DArrayPool()
        {
            m_Pool = new LeasedBufferPool<Texture2dArrayDescriptor, ManagedTexture2DArray>(
                createFunc: desc => new ManagedTexture2DArray(desc),
                releaseFunc: buffer => buffer.Release()
            );
        }
    }
}