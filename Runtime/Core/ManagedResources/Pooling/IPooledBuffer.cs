using System;

namespace Rayforge.Core.ManagedResources.Pooling
{
    /// <summary>
    /// Interface for pooled GPU/graphics resources.
    /// Provides access to the descriptor and lifecycle management (release).
    /// </summary>
    /// <typeparam name="TDesc">Descriptor type describing the resource properties.</typeparam>
    public interface IPooledBuffer<TDesc> : IDisposable
        where TDesc : unmanaged, IEquatable<TDesc>
    {
        /// <summary>
        /// Descriptor describing the resource properties.
        /// </summary>
        public TDesc Descriptor { get; }

        /// <summary>
        /// Releases the resource without disposing the wrapper itself.
        /// </summary>
        public void Release();
    }
}