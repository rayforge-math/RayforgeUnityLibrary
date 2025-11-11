using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Rayforge.ManagedResources.CustomBuffers
{
    /// <summary>
    /// Interface for pooled GPU/graphics resources.
    /// Provides access to the descriptor and lifecycle management (release).
    /// </summary>
    /// <typeparam name="Tdesc">Descriptor type describing the resource properties.</typeparam>
    public interface IPooledBuffer<Tdesc> : IDisposable
        where Tdesc : unmanaged, IEquatable<Tdesc>
    {
        /// <summary>
        /// Descriptor describing the resource properties.
        /// </summary>
        public Tdesc Descriptor { get; }

        /// <summary>
        /// Releases the resource without disposing the wrapper itself.
        /// </summary>
        public void Release();
    }

    /// <summary>
    /// Interface providing access to the underlying internal buffer/resource.
    /// Implemented by managed buffers to expose their raw GPU/system resource.
    /// </summary>
    /// <typeparam name="Tinternal">The internal buffer type (e.g., ComputeBuffer, NativeArray&lt;T&gt;).</typeparam>
    public interface IBufferInternal<Tinternal>
    {
        /// <summary>
        /// The underlying internal GPU/system resource.
        /// </summary>
        public Tinternal Buffer { get; }
    }

    /// <summary>
    /// Interface for descriptors that support batching.
    /// Implementing descriptors expose a <see cref="Count"/> property,
    /// which can be used by a buffer pool to compute batch-aligned allocations.
    /// </summary>
    public interface IBatchingDescriptor
    {
        /// <summary>
        /// The number of elements requested or represented by this descriptor.
        /// Used by buffer pools to round up to batch sizes if necessary.
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// Base class for managed buffers, handling lifecycle and providing access
    /// to both the descriptor and the underlying internal resource.
    /// </summary>
    /// <typeparam name="Tdesc">Descriptor type describing the buffer properties.</typeparam>
    /// <typeparam name="Tinternal">The underlying internal resource type (e.g., ComputeBuffer, NativeArray&lt;T&gt;).</typeparam>
    public abstract class ManagedBuffer<Tdesc, Tinternal> : IPooledBuffer<Tdesc>, IBufferInternal<Tinternal>, IEquatable<ManagedBuffer<Tdesc, Tinternal>>
        where Tdesc : unmanaged, IEquatable<Tdesc>
    {
        /// <summary>
        /// The actual resource being managed (GPU/System buffer).
        /// </summary>
        protected Tinternal m_Buffer;

        /// <summary>
        /// Descriptor describing the resource properties.
        /// </summary>
        protected Tdesc m_Descriptor;

        /// <summary>
        /// Access to the internal resource.
        /// </summary>
        public Tinternal Buffer => m_Buffer;

        /// <summary>
        /// Access to the descriptor from outside.
        /// </summary>
        public Tdesc Descriptor => m_Descriptor;

        /// <summary>
        /// Tracks whether Dispose has been called to avoid double release.
        /// </summary>
        private bool m_Disposed = false;

        /// <summary>
        /// Initializes the managed buffer with a resource and descriptor.
        /// </summary>
        /// <param name="buffer">The internal resource to manage.</param>
        /// <param name="descriptor">Descriptor describing the resource properties.</param>
        public ManagedBuffer(Tinternal buffer, Tdesc descriptor)
        {
            m_Buffer = buffer;
            m_Descriptor = descriptor;
        }

        /// <summary>
        /// Finalizer ensures the resource is released if Dispose was not called.
        /// </summary>
        ~ManagedBuffer() => Dispose(false);

        /// <summary>
        /// Dispose pattern implementation. Calls Dispose(true) and suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Core dispose logic. Calls <see cref="Release"/>.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                Release();
                m_Disposed = true;
            }
        }

        /// <summary>
        /// Default hash code implementation based on the internal buffer.
        /// </summary>
        public override int GetHashCode() => m_Buffer?.GetHashCode() ?? 0;

        /// <summary>
        /// Checks equality with another internal buffer. Must be implemented by derived classes.
        /// </summary>
        public abstract bool Equals(ManagedBuffer<Tdesc, Tinternal> other);

        /// <summary>
        /// Overrides object.Equals to use the type-safe Equals implementation.
        /// Ensures proper behavior in collections like HashSet or Dictionary.
        /// </summary>
        /// <param name="obj">Object to compare with.</param>
        /// <returns>True if equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is ManagedBuffer<Tdesc, Tinternal> other)
                return Equals(other);
            return false;
        }

        /// <summary>
        /// Releases the internal resource (e.g., Dispose NativeArray, Release GPU buffer).
        /// Must be implemented by derived classes.
        /// </summary>
        public abstract void Release();
    }

    /// <summary>
    /// Represents a CPU-side container that exposes backing array data for GPU upload.
    /// Used for data sets such as filter kernels or lookup tables.
    /// </summary>
    /// <typeparam name="Ttype">The unmanaged element type stored in the array.</typeparam>
    public interface IComputeData<Ttype>
        where Ttype : unmanaged
    {
        /// <summary>
        /// Returns the raw array backing the data. This array is directly uploaded to GPU buffers.
        /// </summary>
        public Ttype[] RawData { get; }

        /// <summary>
        /// Returns count of elements in the raw array backing the data for GPU buffer upload.
        /// </summary>
        public int Count { get; }
    }

    /// <summary>
    /// Describes the properties of a compute buffer.
    /// Used to define element count, stride, batch padding, and buffer type.
    /// </summary>
    public struct ComputeBufferDescriptor : IEquatable<ComputeBufferDescriptor>, IBatchingDescriptor
    {
        /// <summary>Number of elements in the buffer.</summary>
        public int count;

        /// <summary>Stride in bytes per element.</summary>
        public int stride;

        /// <summary>Type of the ComputeBuffer (Default, Structured, etc.).</summary>
        public ComputeBufferType type;

        /// <summary>
        /// Number of elements requested in the buffer. 
        /// The pool may use this value along with the batch size to determine actual allocation size.
        /// </summary>
        public int Count
        {
            get => count;
            set => count = value;
        }

        /// <summary>
        /// Checks equality against another <see cref="ComputeBufferDescriptor"/>.
        /// Two descriptors are considered equal if all fields match.
        /// </summary>
        public bool Equals(ComputeBufferDescriptor other)
            => count == other.count
            && stride == other.stride
            && type == other.type;


        /// <summary>
        /// Standard object equality override to ensure correct comparison behavior
        /// when stored in collections such as Dictionary or HashSet.
        /// </summary>
        public override bool Equals(object obj)
            => obj is ComputeBufferDescriptor other && Equals(other);


        /// <summary>
        /// Generates a hash code combining the descriptor fields.
        /// Ensures consistent hashing when used as dictionary keys.
        /// </summary>
        public override int GetHashCode()
            => (count, stride, type).GetHashCode();


        /// <summary>
        /// Equality operator for convenience and clarity.
        /// </summary>
        public static bool operator ==(ComputeBufferDescriptor left, ComputeBufferDescriptor right)
            => left.Equals(right);

        /// <summary>
        /// Inequality operator for convenience and clarity.
        /// </summary>
        public static bool operator !=(ComputeBufferDescriptor left, ComputeBufferDescriptor right)
            => !left.Equals(right);
    }

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
        /// Useful for uniform-style data structures.
        /// </summary>
        public void SetData<T>(IComputeData<T> data)
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

    /// <summary>
    /// Wrapper around Unity's <see cref="RenderTextureDescriptor"/> to provide
    /// value-based comparison and hashing for use in dictionaries and pools.
    /// </summary>
    public struct RenderTextureDescriptorWrapper : IEquatable<RenderTextureDescriptorWrapper>
    {
        /// <summary>The underlying descriptor.</summary>
        public RenderTextureDescriptor descriptor;

        /// <summary>
        /// Compares this wrapper with another wrapper for equality.
        /// </summary>
        public bool Equals(RenderTextureDescriptorWrapper other)
            => Equals(other.descriptor);

        /// <summary>
        /// Compares this wrapper with a raw <see cref="RenderTextureDescriptor"/>.
        /// </summary>
        public bool Equals(RenderTextureDescriptor other)
        {
            return
                other.width == descriptor.width &&
                other.height == descriptor.height &&
                other.colorFormat == descriptor.colorFormat &&
                other.depthBufferBits == descriptor.depthBufferBits &&
                other.dimension == descriptor.dimension &&
                other.volumeDepth == descriptor.volumeDepth &&
                other.msaaSamples == descriptor.msaaSamples &&
                other.useMipMap == descriptor.useMipMap &&
                other.autoGenerateMips == descriptor.autoGenerateMips &&
                other.enableRandomWrite == descriptor.enableRandomWrite &&
                other.useDynamicScale == descriptor.useDynamicScale &&
                other.sRGB == descriptor.sRGB &&
                other.bindMS == descriptor.bindMS;
        }

        /// <summary>
        /// Object equality override.
        /// </summary>
        public override bool Equals(object obj)
            => obj is RenderTextureDescriptorWrapper other && Equals(other);

        /// <summary>
        /// Creates a stable hash code from all relevant descriptor properties.
        /// </summary>
        public override int GetHashCode()
            => (
                descriptor.width,
                descriptor.height,
                descriptor.colorFormat,
                descriptor.depthBufferBits,
                descriptor.dimension,
                descriptor.volumeDepth,
                descriptor.msaaSamples,
                descriptor.useMipMap,
                descriptor.autoGenerateMips,
                descriptor.enableRandomWrite,
                descriptor.useDynamicScale,
                descriptor.sRGB,
                descriptor.bindMS
            ).GetHashCode();

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(RenderTextureDescriptorWrapper left, RenderTextureDescriptorWrapper right)
            => left.Equals(right);

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(RenderTextureDescriptorWrapper left, RenderTextureDescriptorWrapper right)
            => !left.Equals(right);
    }

    /// <summary>
    /// Managed wrapper around <see cref="RenderTexture"/> that ensures proper creation,
    /// configuration, and disposal. Inherits from <see cref="ManagedBuffer{TDesc, TBuffer}"/>.
    /// </summary>
    public sealed class ManagedRenderTexture : ManagedBuffer<RenderTextureDescriptorWrapper, RenderTexture>
    {
        /// <summary>
        /// Creates and configures a managed render texture.
        /// </summary>
        /// <param name="desc">Descriptor defining resolution, format, and other texture properties.</param>
        /// <param name="filterMode">Filter mode (Point, Bilinear, Trilinear) for sampling.</param>
        /// <param name="wrapMode">Wrap mode (Clamp, Repeat, Mirror) for texture coordinates.</param>
        public ManagedRenderTexture(RenderTextureDescriptorWrapper desc, FilterMode filterMode, TextureWrapMode wrapMode)
            : base(CreateAndConfigureTexture(desc, filterMode, wrapMode), desc)
        { }

        /// <summary>
        /// Creates the <see cref="RenderTexture"/> from the descriptor and applies filtering and wrapping.
        /// </summary>
        private static RenderTexture CreateAndConfigureTexture(RenderTextureDescriptorWrapper desc, FilterMode filterMode, TextureWrapMode wrapMode)
        {
            var texture = new RenderTexture(desc.descriptor)
            {
                filterMode = filterMode,
                wrapMode = wrapMode
            };
            texture.Create(); // Allocate GPU memory
            return texture;
        }

        /// <summary>
        /// Releases the underlying GPU render texture and clears internal references.
        /// After this call, the texture is no longer valid.
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
        /// Compares managed render textures by reference. Useful for pooling or resource tracking.
        /// </summary>
        public override bool Equals(ManagedBuffer<RenderTextureDescriptorWrapper, RenderTexture> other)
            => ReferenceEquals(this, other);
    }

    /// <summary>
    /// Descriptor for a 2D texture, containing resolution, pixel format,
    /// mipmap configuration, and sampling/filtering settings.
    /// Used as the configuration key for texture pooling.
    /// </summary>
    public struct Texture2dDescriptor : IEquatable<Texture2dDescriptor>
    {
        public int width;
        public int height;
        public TextureFormat colorFormat;
        public int mipCount;
        public bool linear;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;

        /// <summary>
        /// Compares all descriptor fields for equality.
        /// </summary>
        public bool Equals(Texture2dDescriptor other)
            => width == other.width
            && height == other.height
            && colorFormat == other.colorFormat
            && mipCount == other.mipCount
            && linear == other.linear
            && filterMode == other.filterMode
            && wrapMode == other.wrapMode;

        /// <summary>
        /// Object override to ensure proper equality handling when stored
        /// in collections such as <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </summary>
        public override bool Equals(object obj)
            => obj is Texture2dDescriptor other && Equals(other);

        /// <summary>
        /// Generates a stable hash from all fields so the descriptor can
        /// be safely used as a dictionary or hash set key.
        /// </summary>
        public override int GetHashCode()
            => (width, height, colorFormat, mipCount, linear, filterMode, wrapMode).GetHashCode();

        /// <summary>Equality operator for convenience.</summary>
        public static bool operator ==(Texture2dDescriptor left, Texture2dDescriptor right)
            => left.Equals(right);

        /// <summary>Inequality operator for convenience.</summary>
        public static bool operator !=(Texture2dDescriptor left, Texture2dDescriptor right)
            => !left.Equals(right);
    }

    /// <summary>
    /// Managed wrapper around Unity's <see cref="Texture2D"/>.
    /// Provides creation, configuration, and controlled release for pooling or resource tracking.
    /// Inherits from <see cref="ManagedBuffer{TBuffer,TDesc}"/>.
    /// </summary>
    public sealed class ManagedTexture2D : ManagedBuffer<Texture2dDescriptor, Texture2D>
    {
        /// <summary>Width of the texture.</summary>
        public int Width => m_Descriptor.width;

        /// <summary>Height of the texture.</summary>
        public int Height => m_Descriptor.height;

        /// <summary>
        /// Creates a managed Texture2D with the provided descriptor.
        /// </summary>
        public ManagedTexture2D(Texture2dDescriptor desc)
            : base(CreateAndConfigureTexture(desc), desc)
        { }

        /// <summary>
        /// Instantiates the actual Texture2D object based on the descriptor.
        /// Configures filter and wrap modes.
        /// </summary>
        private static Texture2D CreateAndConfigureTexture(Texture2dDescriptor desc)
        {
            return new Texture2D(desc.width, desc.height, desc.colorFormat, desc.mipCount, desc.linear)
            {
                filterMode = desc.filterMode,
                wrapMode = desc.wrapMode
            };
        }

        /// <summary>
        /// Releases the underlying texture. After this call, the texture is no longer valid.
        /// Note: does not destroy the wrapper itself, enabling pooling or reuse.
        /// </summary>
        public override void Release()
            => m_Buffer = null;

        /// <summary>
        /// Compares managed textures by reference. Suitable for pooling or tracking.
        /// </summary>
        public override bool Equals(ManagedBuffer<Texture2dDescriptor, Texture2D> other)
            => ReferenceEquals(this, other);
    }

    /// <summary>
    /// Descriptor for a 2D texture array, including the base texture descriptor
    /// and the number of array slices. Acts as a hashing key for texture array pooling.
    /// </summary>
    public struct Texture2dArrayDescriptor : IEquatable<Texture2dArrayDescriptor>
    {
        /// <summary>
        /// Descriptor that defines width, height, format and sampling settings
        /// for each texture in the array.
        /// </summary>
        public Texture2dDescriptor descriptor;

        /// <summary>
        /// Number of texture layers in the array.
        /// </summary>
        public int count;

        /// <summary>
        /// Compares both the inner descriptor and the array layer count.
        /// </summary>
        public bool Equals(Texture2dArrayDescriptor other)
            => descriptor.Equals(other.descriptor)
            && count == other.count;

        /// <summary>
        /// Ensures compatibility with object-based comparisons.
        /// </summary>
        public override bool Equals(object obj)
            => obj is Texture2dArrayDescriptor other && Equals(other);

        /// <summary>
        /// Computes a stable hash for dictionary / hash set usage.
        /// </summary>
        public override int GetHashCode()
            => (descriptor, count).GetHashCode();

        /// <summary>Equality operator.</summary>
        public static bool operator ==(Texture2dArrayDescriptor left, Texture2dArrayDescriptor right)
            => left.Equals(right);

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(Texture2dArrayDescriptor left, Texture2dArrayDescriptor right)
            => !left.Equals(right);
    }

    /// <summary>
    /// Managed wrapper around Unity's <see cref="Texture2DArray"/>.
    /// Provides creation, validation, and controlled release.
    /// </summary>
    public sealed class ManagedTexture2DArray : ManagedBuffer<Texture2dArrayDescriptor, Texture2DArray>
    {
        public ManagedTexture2DArray(Texture2dArrayDescriptor descriptor)
            : base(CreateAndConfigure(descriptor), descriptor)
        { }

        /// <summary>
        /// Releases the underlying texture array.
        /// </summary>
        public override void Release()
        {
            if (m_Buffer != null)
            {
                Texture2DArray.Destroy(m_Buffer);
                m_Buffer = null;
            }
        }

        /// <summary>
        /// Instantiates and configures the Texture2DArray based on the descriptor.
        /// </summary>
        private static Texture2DArray CreateAndConfigure(Texture2dArrayDescriptor desc)
        {
            if (desc.count <= 0)
                throw new ArgumentException("Texture2DArray count must be > 0");
            if (desc.descriptor.width <= 0 || desc.descriptor.height <= 0)
                throw new ArgumentException("Texture dimensions must be > 0");

            var texture = new Texture2DArray(
                desc.descriptor.width,
                desc.descriptor.height,
                desc.count,
                desc.descriptor.colorFormat,
                desc.descriptor.mipCount > 1,
                desc.descriptor.linear
            );

            texture.filterMode = desc.descriptor.filterMode;
            texture.wrapMode = desc.descriptor.wrapMode;
            texture.anisoLevel = 0;
            texture.Apply(false);

            return texture;
        }

        /// <summary>
        /// Copies the provided textures into the array.
        /// Validates dimensions, format, and mip count before copying.
        /// </summary>
        public bool SetTextures(Texture2D[] textures)
        {
            if (textures == null || textures.Length == 0)
            {
                Debug.LogError("Texture array is null or empty");
                return false;
            }

            var descriptor = m_Descriptor.descriptor;
            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i] == null)
                {
                    Debug.LogError($"Texture at index {i} is null");
                    return false;
                }

                if (textures[i].width != descriptor.width || textures[i].height != descriptor.height)
                {
                    Debug.LogError($"Texture at index {i} has mismatched dimensions. " +
                                $"Expected: {descriptor.width}x{descriptor.height}, Got: {textures[i].width}x{textures[i].height}");
                    return false;
                }

                if (textures[i].format != descriptor.colorFormat)
                {
                    Debug.LogWarning($"Texture at index {i} has format {textures[i].format}, " +
                                    $"but expected {descriptor.colorFormat}. This may cause conversion overhead.");
                }

                if (textures[i].mipmapCount != descriptor.mipCount)
                {
                    Debug.LogWarning($"Texture at index {i} has mipmap count {textures[i].mipmapCount}, " +
                                    $"but expected {descriptor.mipCount}. This may result in faulty graphics.");
                }
            }

            if (textures.Length > m_Descriptor.count)
            {
                Debug.LogWarning($"More textures ({textures.Length}) provided than array size ({m_Descriptor.count}). " +
                                $"Only the first {m_Descriptor.count} will be used.");
            }

            try
            {
                int texturesToCopy = Mathf.Min(textures.Length, m_Descriptor.count);
                for (int i = 0; i < texturesToCopy; i++)
                {
                    for (int j = 0; j < descriptor.mipCount; ++j)
                    {
                        try
                        {
                            Graphics.CopyTexture(textures[i], 0, j, m_Buffer, i, j);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Copy texture {i} MipMap {j}: {ex}");
                        }
                    }
                }

                m_Buffer.Apply(false);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to copy textures: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Compares managed texture arrays by reference.
        /// Suitable for pooling or tracking.
        /// </summary>
        public override bool Equals(ManagedBuffer<Texture2dArrayDescriptor, Texture2DArray> other)
            => ReferenceEquals(this, other);
    }

    /// <summary>
    /// Descriptor for a native system buffer (NativeArray), including size and allocator.
    /// </summary>
    public struct SystemBufferDescriptor : IEquatable<SystemBufferDescriptor>, IBatchingDescriptor
    {
        /// <summary>Number of elements in the buffer.</summary>
        public int count;

        /// <summary>Allocator used for the NativeArray.</summary>
        public Allocator allocator;

        /// <summary>
        /// Number of elements requested in the buffer. 
        /// The pool may use this value along with the batch size to determine actual allocation size.
        /// </summary>
        public int Count
        {
            get => count;
            set => count = value;
        }

        /// <summary>
        /// Compares two descriptors for equality.
        /// </summary>
        public bool Equals(SystemBufferDescriptor other)
            => count == other.count && allocator == other.allocator;

        /// <summary>
        /// Overrides object.Equals to match IEquatable implementation.
        /// </summary>
        public override bool Equals(object obj)
            => obj is SystemBufferDescriptor other && Equals(other);

        /// <summary>
        /// Provides a hash code for use in dictionaries or hash sets.
        /// </summary>
        public override int GetHashCode()
            => (count, allocator).GetHashCode();

        /// <summary>Equality operator.</summary>
        public static bool operator ==(SystemBufferDescriptor lhs, SystemBufferDescriptor rhs)
            => lhs.Equals(rhs);

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(SystemBufferDescriptor lhs, SystemBufferDescriptor rhs)
            => !lhs.Equals(rhs);
    }

    /// <summary>
    /// Managed wrapper around a <see cref="NativeArray{T}"/>.
    /// Provides automatic creation, release, and pooling support.
    /// </summary>
    /// <typeparam name="T">The struct type stored in the array.</typeparam>
    public sealed class ManagedSystemBuffer<T> : ManagedBuffer<SystemBufferDescriptor, NativeArray<T>>
        where T : struct
    {
        public ManagedSystemBuffer(SystemBufferDescriptor desc)
            : base(new NativeArray<T>(desc.count, desc.allocator), desc)
        { }

        /// <summary>
        /// Compares managed system buffers by reference.
        /// </summary>
        public override bool Equals(ManagedBuffer<SystemBufferDescriptor, NativeArray<T>> other)
            => ReferenceEquals(this, other);

        /// <summary>
        /// Releases the underlying NativeArray memory.
        /// </summary>
        public override void Release()
            => m_Buffer.Dispose();
    }
}