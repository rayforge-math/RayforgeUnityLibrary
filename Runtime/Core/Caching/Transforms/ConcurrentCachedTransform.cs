using Rayforge.Caching.Abstractions;
using UnityEngine;

namespace Rayforge.Caching.Transforms
{
    /// <summary>
    /// Thread-safe variant of <see cref="CachedTransform"/>.
    /// Wraps all cache and Transform accessors with a synchronization lock,
    /// allowing safe multi-threaded reads/writes to cached transform data.
    ///
    /// Note: Unity's Transform API is not thread-safe — only cached values are safe
    /// to access from background threads. Direct UnityEngine.Transform operations
    /// (through <see cref="Self"/>) must still occur on the main thread.
    /// </summary>
    public class ConcurrentCachedTransform : CachedTransform
    {
        /// <summary>
        /// Lock object used to synchronize access to cached state and Unity Transform operations.
        /// </summary>
        private readonly object m_Lock = new();

        /// <summary>
        /// Initializes a new instance of <see cref="ConcurrentCachedTransform"/> using the given GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to wrap with caching and locking.</param>
        public ConcurrentCachedTransform(GameObject gameObject)
            : base(gameObject)
        { }

        /// <inheritdoc/>
        public override Vector3 Position
        {
            get
            {
                lock (m_Lock)
                    return base.Position;
            }
            set
            {
                lock (m_Lock)
                    base.Position = value;
            }
        }

        /// <inheritdoc/>
        public override Quaternion Rotation
        {
            get
            {
                lock (m_Lock)
                    return base.Rotation;
            }
            set
            {
                lock (m_Lock)
                    base.Rotation = value;
            }
        }

        /// <inheritdoc/>
        public override Vector3 Scale
        {
            get
            {
                lock (m_Lock)
                    return base.Scale;
            }
            set
            {
                lock (m_Lock)
                    base.Scale = value;
            }
        }

        /// <inheritdoc/>
        public override ICachedTransform Parent
        {
            get
            {
                lock (m_Lock)
                    return base.Parent;
            }
            set
            {
                lock (m_Lock)
                    base.Parent = value;
            }
        }

        /// <inheritdoc/>
        public override void SetParent(ICachedTransform parent, bool worldPositionStays = false)
        {
            lock (m_Lock)
                base.SetParent(parent, worldPositionStays);
        }

        /// <summary>
        /// Refreshes the cached data from the Unity Transform in a thread-safe way.
        /// Note: Still must be called on the main thread to access Unity objects safely.
        /// </summary>
        public override void Refresh()
        {
            lock (m_Lock)
                base.Refresh();
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            lock (m_Lock)
                base.Dispose();
        }
    }
}