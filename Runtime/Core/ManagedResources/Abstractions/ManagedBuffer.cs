using Rayforge.ManagedResources.NativeMemory;
using Rayforge.ManagedResources.Pooling;

using System;

namespace Rayforge.ManagedResources.Abstractions
{
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
}