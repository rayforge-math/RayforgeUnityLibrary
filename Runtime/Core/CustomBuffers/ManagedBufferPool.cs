using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

using Rayforge.ManagedResources.CustomBuffers;

namespace Rayforge.ManagedResources.CustomBufferPools
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
    public class LeasedBuffer<Tdesc, Tbuffer>
        where Tbuffer : IPooledBuffer<Tdesc>
        where Tdesc : unmanaged, IEquatable<Tdesc>
    {
        private Tbuffer m_BufferHandle;
        private LeasedReturnFunc<Tdesc, Tbuffer> m_OnReturn;
        private bool m_Valid;

        /// <summary>
        /// Gets the underlying buffer. Throws an exception if the buffer has already been returned.
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
        /// Creates a new leased buffer.
        /// </summary>
        /// <param name="buffer">The actual buffer being leased.</param>
        /// <param name="onReturnHandle">Callback invoked when the buffer is returned to the pool.</param>
        public LeasedBuffer(Tbuffer buffer, LeasedReturnFunc<Tdesc, Tbuffer> onReturnHandle)
        {
            m_BufferHandle = buffer;
            m_OnReturn = onReturnHandle;
            m_Valid = true;
        }

        /// <summary>
        /// Returns the buffer to the pool. After returning, any access to the buffer will throw an exception.
        /// </summary>
        /// <returns>True if the buffer was successfully returned; false if it was already returned.</returns>
        public bool Return()
        {
            if (!m_Valid)
                return false;

            m_Valid = false;

            if (m_OnReturn == null)
            {
                if (m_BufferHandle == null)
                {
                    return false;
                }
                else
                {
                    throw new InvalidOperationException("Return delegate is not set.");
                }
            }

            return m_OnReturn.Invoke(m_BufferHandle);
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
    /// Generic buffer pool that manages lifetime of pooled buffer objects.
    /// This pool is *not* thread-safe. Thread-safety, eviction policies, and auto-cleanup
    /// must be implemented by deriving from this class.
    /// </summary>
    /// <typeparam name="Tdesc">Descriptor type used to categorize buffers.</typeparam>
    /// <typeparam name="Tbuffer">Pooled buffer type.</typeparam>
    public class LeasedBufferPool<Tdesc, Tbuffer> : IDisposable
        where Tbuffer : IPooledBuffer<Tdesc>
        where Tdesc : unmanaged, IEquatable<Tdesc>
    {
        /// <summary>Factory used to create new buffers on demand.</summary>
        protected readonly BufferCreateFunc<Tdesc, Tbuffer> m_CreateFunc;

        /// <summary>Factory used to permanently release a buffer.</summary>
        protected readonly BufferReleaseFunc<Tdesc, Tbuffer> m_ReleaseFunc;

        /// <summary>Free buffers grouped by descriptor.</summary>
        protected readonly Dictionary<Tdesc, Stack<Tbuffer>> m_FreeDict = new();

        /// <summary>Buffers currently checked out.</summary>
        protected readonly HashSet<Tbuffer> m_Reserved = new();

        public LeasedBufferPool(
            BufferCreateFunc<Tdesc, Tbuffer> createFunc,
            BufferReleaseFunc<Tdesc, Tbuffer> releaseFunc)
        {
            m_CreateFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            m_ReleaseFunc = releaseFunc ?? throw new ArgumentNullException(nameof(releaseFunc));
        }

        /// <summary>
        /// Rents a buffer from the pool. May create a new one if none are available.
        /// Override in derived classes to control concurrency or allocation policy.
        /// </summary>
        public virtual LeasedBuffer<Tdesc, Tbuffer> Rent(Tdesc desc)
        {
            Tbuffer buffer;

            if (m_FreeDict.TryGetValue(desc, out var stack) && stack.Count > 0)
            {
                buffer = stack.Pop();
                m_Reserved.Add(buffer);
            }
            else
            {
                buffer = m_CreateFunc.Invoke(desc);
                m_Reserved.Add(buffer);
            }

            return new LeasedBuffer<Tdesc, Tbuffer>(buffer, Return);
        }

        /// <summary>
        /// Returns a buffer to the pool. Override for thread-safety or reset operations.
        /// </summary>
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
        /// Permanently releases all pooled buffers. Override to control teardown behavior.
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
    }

    /// <summary>
    /// A specialized buffer pool that supports batching for sequential buffers.
    /// Useful for ComputeBuffers or NativeArrays where allocations can be rounded
    /// to a batch size to reduce frequent reallocations.
    /// </summary>
    /// <typeparam name="Tdesc">Type of the descriptor describing the buffer properties.</typeparam>
    /// <typeparam name="Tbuffer">Type of the managed buffer (e.g., ManagedComputeBuffer).</typeparam>
    public class BatchedLeasedBufferPool<Tdesc, Tbuffer> : LeasedBufferPool<Tdesc, Tbuffer>
        where Tbuffer : IPooledBuffer<Tdesc>
        where Tdesc : unmanaged, IEquatable<Tdesc>, IBatchingDescriptor
    {
        /// <summary>
        /// Minimum allocation size to ensure a base buffer size.
        /// </summary>
        public int BaseSize { get; }

        /// <summary>
        /// Batch size for rounding allocations.
        /// </summary>
        public int BatchSize { get; }

        /// <summary>
        /// Constructs a new batched buffer pool.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer when the pool is empty.</param>
        /// <param name="releaseFunc">Function to release a buffer permanently.</param>
        /// <param name="baseSize">Minimum base allocation size.</param>
        /// <param name="batchSize">Batch size for rounding allocations. If set to 0, batching is disabled.</param>
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
        /// Rents a buffer from the pool using batch-adjusted sizing.
        /// </summary>
        /// <param name="desc">Descriptor used to identify or create the buffer.</param>
        /// <returns>A <see cref="LeasedBuffer{Tdesc, Tbuffer}"/> representing the rented buffer.</returns>
        public override LeasedBuffer<Tdesc, Tbuffer> Rent(Tdesc desc)
        {
            desc.Count = BatchedCount(desc.Count);
            return base.Rent(desc);
        }
    }

    /// <summary>
    /// Global static pool for <see cref="ManagedComputeBuffer"/> instances.
    /// Provides convenient access to a shared buffer pool and factory method for custom pools.
    /// </summary>
    public static class ManagedComputeBufferPool
    {
        /// <summary>
        /// Internal static pool instance used for the default Rent() method.
        /// </summary>
        private static readonly LeasedBufferPool<ComputeBufferDescriptor, ManagedComputeBuffer> m_Pool;

        /// <summary>
        /// Static constructor initializes the global pool.
        /// </summary>
        static ManagedComputeBufferPool()
        {
            m_Pool = new LeasedBufferPool<ComputeBufferDescriptor, ManagedComputeBuffer>(
                createFunc: (desc) => new ManagedComputeBuffer(desc),
                releaseFunc: (buffer) => buffer.Dispose()
            );
        }

        /// <summary>
        /// Rents a buffer from the global pool.
        /// The returned buffer is wrapped in a <see cref="LeasedBuffer{Tdesc,Tbuffer}"/> and automatically returned when disposed.
        /// </summary>
        /// <param name="desc">Descriptor describing the desired compute buffer.</param>
        /// <returns>A leased buffer representing the rented <see cref="ManagedComputeBuffer"/>.</returns>
        public static LeasedBuffer<ComputeBufferDescriptor, ManagedComputeBuffer> Rent(ComputeBufferDescriptor desc)
            => m_Pool.Rent(desc);

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

        /// <summary>
        /// Creates a custom LeasedBufferPool with user-provided factory methods.
        /// </summary>
        /// <param name="createFunc">Factory method to create new buffers when needed.</param>
        /// <param name="releaseFunc">Callback to release buffers permanently.</param>
        /// <returns>A new LeasedBufferPool instance.</returns>
        public static LeasedBufferPool<ComputeBufferDescriptor, ManagedComputeBuffer> Create(
            BufferCreateFunc<ComputeBufferDescriptor, ManagedComputeBuffer> createFunc,
            BufferReleaseFunc<ComputeBufferDescriptor, ManagedComputeBuffer> releaseFunc)
        {
            return new LeasedBufferPool<ComputeBufferDescriptor, ManagedComputeBuffer>(
                createFunc,
                releaseFunc
            );
        }

        /// <summary>
        /// Creates a batched buffer pool for sequential buffers, with optional base and batch sizes.
        /// Useful to reduce frequent reallocations when the buffer size grows.
        /// </summary>
        /// <param name="createFunc">Factory method to create new buffers when needed.</param>
        /// <param name="releaseFunc">Callback to release buffers permanently.</param>
        /// <param name="baseSize">Minimum allocation size (defaults to 1).</param>
        /// <param name="batchSize">Batch size for rounding allocations (0 disables batching).</param>
        /// <returns>A new BatchedLeasedBufferPool instance.</returns>
        public static BatchedLeasedBufferPool<ComputeBufferDescriptor, ManagedComputeBuffer> CreateBatched(
            BufferCreateFunc<ComputeBufferDescriptor, ManagedComputeBuffer> createFunc,
            BufferReleaseFunc<ComputeBufferDescriptor, ManagedComputeBuffer> releaseFunc, 
            int baseSize = 1,
            int batchSize = 0)
        {
            return new BatchedLeasedBufferPool<ComputeBufferDescriptor, ManagedComputeBuffer>(
                createFunc,
                releaseFunc,
                baseSize,
                batchSize
            );
        }
    }

    /// <summary>
    /// Global static access to a pool of managed system buffers (<see cref="NativeArray{T}"/>).
    /// Provides simple Rent() for default use and factory methods for custom pools,
    /// including a batched variant for sequential buffers to reduce frequent reallocations.
    /// </summary>
    /// <typeparam name="T">The struct type stored in the NativeArray.</typeparam>
    public static class ManagedSystemBufferPool<T>
        where T : struct
    {
        /// <summary>
        /// Default global pool instance.
        /// </summary>
        private static readonly LeasedBufferPool<SystemBufferDescriptor, ManagedSystemBuffer<T>> m_Pool;

        /// <summary>
        /// Static constructor initializes the default global pool.
        /// </summary>
        static ManagedSystemBufferPool()
        {
            m_Pool = new LeasedBufferPool<SystemBufferDescriptor, ManagedSystemBuffer<T>>(
                createFunc: desc => new ManagedSystemBuffer<T>(desc),
                releaseFunc: buffer => buffer.Release()
            );
        }

        /// <summary>
        /// Rent a system buffer from the default global pool.
        /// </summary>
        /// <param name="desc">Descriptor for the buffer to rent.</param>
        /// <returns>A leased buffer managing the lifetime automatically.</returns>
        public static LeasedBuffer<SystemBufferDescriptor, ManagedSystemBuffer<T>> Rent(SystemBufferDescriptor desc)
            => m_Pool.Rent(desc);

        /// <summary>
        /// Creates a custom LeasedBufferPool with user-provided factory methods.
        /// </summary>
        /// <param name="createFunc">Factory method to create new buffers when needed.</param>
        /// <param name="releaseFunc">Callback to release buffers permanently.</param>
        /// <returns>A new LeasedBufferPool instance.</returns>
        public static LeasedBufferPool<SystemBufferDescriptor, ManagedSystemBuffer<T>> Create(
            BufferCreateFunc<SystemBufferDescriptor, ManagedSystemBuffer<T>> createFunc,
            BufferReleaseFunc<SystemBufferDescriptor, ManagedSystemBuffer<T>> releaseFunc)
        {
            return new LeasedBufferPool<SystemBufferDescriptor, ManagedSystemBuffer<T>>(
                createFunc,
                releaseFunc
            );
        }

        /// <summary>
        /// Creates a batched buffer pool for sequential buffers, with optional base and batch sizes.
        /// Useful to reduce frequent reallocations when the buffer size grows.
        /// </summary>
        /// <param name="createFunc">Factory method to create new buffers when needed.</param>
        /// <param name="releaseFunc">Callback to release buffers permanently.</param>
        /// <param name="baseSize">Minimum allocation size (defaults to 1).</param>
        /// <param name="batchSize">Batch size for rounding allocations (0 disables batching).</param>
        /// <returns>A new BatchedLeasedBufferPool instance.</returns>
        public static BatchedLeasedBufferPool<SystemBufferDescriptor, ManagedSystemBuffer<T>> CreateBatched(
            BufferCreateFunc<SystemBufferDescriptor, ManagedSystemBuffer<T>> createFunc,
            BufferReleaseFunc<SystemBufferDescriptor, ManagedSystemBuffer<T>> releaseFunc,
            int baseSize = 1,
            int batchSize = 0)
        {
            return new BatchedLeasedBufferPool<SystemBufferDescriptor, ManagedSystemBuffer<T>>(
                createFunc,
                releaseFunc,
                baseSize,
                batchSize
            );
        }
    }

    /// <summary>
    /// Global static access to a pool of managed render textures.
    /// Provides simple Rent() for default use and factory methods for custom pools.
    /// </summary>
    public static class ManagedRenderTexturePool
    {
        /// <summary>
        /// Default global pool instance.
        /// </summary>
        private static readonly LeasedBufferPool<RenderTextureDescriptorWrapper, ManagedRenderTexture> m_Pool;

        /// <summary>
        /// Static constructor initializes the default global pool.
        /// </summary>
        static ManagedRenderTexturePool()
        {
            m_Pool = new LeasedBufferPool<RenderTextureDescriptorWrapper, ManagedRenderTexture>(
                createFunc: desc => new ManagedRenderTexture(desc, FilterMode.Bilinear, TextureWrapMode.Clamp),
                releaseFunc: buffer => buffer.Release()
            );
        }

        /// <summary>
        /// Rent a managed render texture from the default global pool.
        /// </summary>
        /// <param name="desc">Descriptor for the render texture to rent.</param>
        /// <returns>A leased buffer managing the lifetime automatically.</returns>
        public static LeasedBuffer<RenderTextureDescriptorWrapper, ManagedRenderTexture> Rent(RenderTextureDescriptorWrapper desc)
            => m_Pool.Rent(desc);

        /// <summary>
        /// Creates a custom LeasedBufferPool with user-provided factory methods.
        /// </summary>
        /// <param name="createFunc">Factory method to create new render textures when needed.</param>
        /// <param name="releaseFunc">Callback to release render textures permanently.</param>
        /// <returns>A new LeasedBufferPool instance.</returns>
        public static LeasedBufferPool<RenderTextureDescriptorWrapper, ManagedRenderTexture> Create(
            BufferCreateFunc<RenderTextureDescriptorWrapper, ManagedRenderTexture> createFunc,
            BufferReleaseFunc<RenderTextureDescriptorWrapper, ManagedRenderTexture> releaseFunc)
        {
            return new LeasedBufferPool<RenderTextureDescriptorWrapper, ManagedRenderTexture>(
                createFunc,
                releaseFunc
            );
        }
    }

    /// <summary>
    /// Global static access to a pool of managed Texture2D objects.
    /// Provides simple Rent() for default use and factory methods for custom pools.
    /// </summary>
    public static class ManagedTexture2DPool
    {
        /// <summary>
        /// Default global pool instance.
        /// </summary>
        private static readonly LeasedBufferPool<Texture2dDescriptor, ManagedTexture2D> m_Pool;

        /// <summary>
        /// Static constructor initializes the default global pool.
        /// </summary>
        static ManagedTexture2DPool()
        {
            m_Pool = new LeasedBufferPool<Texture2dDescriptor, ManagedTexture2D>(
                createFunc: desc => new ManagedTexture2D(desc),
                releaseFunc: buffer => buffer.Release()
            );
        }

        /// <summary>
        /// Rent a managed Texture2D from the default global pool.
        /// </summary>
        /// <param name="desc">Descriptor for the Texture2D to rent.</param>
        /// <returns>A leased buffer managing the lifetime automatically.</returns>
        public static LeasedBuffer<Texture2dDescriptor, ManagedTexture2D> Rent(Texture2dDescriptor desc)
            => m_Pool.Rent(desc);

        /// <summary>
        /// Creates a custom LeasedBufferPool with user-provided factory methods.
        /// </summary>
        /// <param name="createFunc">Factory method to create new Texture2D objects when needed.</param>
        /// <param name="releaseFunc">Callback to release textures permanently.</param>
        /// <returns>A new LeasedBufferPool instance.</returns>
        public static LeasedBufferPool<Texture2dDescriptor, ManagedTexture2D> Create(
            BufferCreateFunc<Texture2dDescriptor, ManagedTexture2D> createFunc,
            BufferReleaseFunc<Texture2dDescriptor, ManagedTexture2D> releaseFunc)
        {
            return new LeasedBufferPool<Texture2dDescriptor, ManagedTexture2D>(
                createFunc,
                releaseFunc
            );
        }
    }

    /// <summary>
    /// Global static access to a pool of managed Texture2DArray objects.
    /// Provides Rent() for default use and factory method to create custom pools.
    /// </summary>
    public static class ManagedTexture2DArrayPool
    {
        /// <summary>
        /// Default global pool instance.
        /// </summary>
        private static readonly LeasedBufferPool<Texture2dArrayDescriptor, ManagedTexture2DArray> m_Pool;

        /// <summary>
        /// Static constructor initializes the default global pool.
        /// </summary>
        static ManagedTexture2DArrayPool()
        {
            m_Pool = new LeasedBufferPool<Texture2dArrayDescriptor, ManagedTexture2DArray>(
                createFunc: desc => new ManagedTexture2DArray(desc),
                releaseFunc: buffer => buffer.Release()
            );
        }

        /// <summary>
        /// Rent a managed Texture2DArray from the default global pool.
        /// </summary>
        /// <param name="desc">Descriptor for the Texture2DArray to rent.</param>
        /// <returns>A leased buffer managing the lifetime automatically.</returns>
        public static LeasedBuffer<Texture2dArrayDescriptor, ManagedTexture2DArray> Rent(Texture2dArrayDescriptor desc)
            => m_Pool.Rent(desc);

        /// <summary>
        /// Creates a custom LeasedBufferPool with user-provided factory methods.
        /// </summary>
        /// <param name="createFunc">Factory method to create new Texture2DArray objects.</param>
        /// <param name="releaseFunc">Callback to release textures permanently.</param>
        /// <returns>A new LeasedBufferPool instance.</returns>
        public static LeasedBufferPool<Texture2dArrayDescriptor, ManagedTexture2DArray> Create(
            BufferCreateFunc<Texture2dArrayDescriptor, ManagedTexture2DArray> createFunc,
            BufferReleaseFunc<Texture2dArrayDescriptor, ManagedTexture2DArray> releaseFunc)
        {
            return new LeasedBufferPool<Texture2dArrayDescriptor, ManagedTexture2DArray>(
                createFunc,
                releaseFunc
            );
        }
    }
}