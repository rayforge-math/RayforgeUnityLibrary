using System;

namespace Rayforge.ManagedResources.Pooling
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
}