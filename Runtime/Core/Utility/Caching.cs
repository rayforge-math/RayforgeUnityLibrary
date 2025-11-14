using System;
using UnityEngine;

namespace Rayforge.Utility.Caching
{
    /// <summary>
    /// Defines a lightweight abstraction layer over Unity's <see cref="Transform"/> component,
    /// providing cached access to position, rotation, scale, and parenting relationships.
    ///
    /// This interface allows safer use of transforms in systems where direct access to
    /// UnityEngine.Transform is undesirable — for example, in multi-threaded or data-oriented contexts.
    /// </summary>
    public interface ITransform : IDisposable
    {
        /// <summary>
        /// Gets or sets the world-space position of this transform.
        /// </summary>
        Vector3 Position { get; set; }

        /// <summary>
        /// Gets or sets the world-space rotation of this transform.
        /// </summary>
        Quaternion Rotation { get; set; }

        /// <summary>
        /// Gets or sets the local scale of this transform.
        /// </summary>
        Vector3 Scale { get; set; }

        /// <summary>
        /// Gets or sets the parent of this transform in the cached transform hierarchy.
        /// Setting this value automatically updates the underlying Unity transform.
        /// </summary>
        ITransform Parent { get; set; }

        /// <summary>
        /// Gets the underlying Unity <see cref="Transform"/> instance.
        /// 
        /// Use this property only when direct Unity API access is required.
        /// For general use, prefer the <see cref="ITransform"/> abstraction instead.
        /// </summary>
        Transform Self { get; }

        /// <summary>
        /// Sets the parent transform, optionally preserving the current world position.
        /// </summary>
        /// <param name="parent">The new parent transform, or <see langword="null"/> to unparent.</param>
        /// <param name="worldPositionStays">
        /// If <see langword="true"/>, the transform keeps its current world position and rotation.
        /// If <see langword="false"/>, it is re-aligned to the local space of the new parent.
        /// </param>
        void SetParent(ITransform parent, bool worldPositionStays = false);

        /// <summary>
        /// Adds a new Unity <see cref="Component"/> of type <typeparamref name="Tcomp"/> to the underlying GameObject.
        /// </summary>
        /// <typeparam name="Tcomp">The type of component to add.</typeparam>
        /// <returns>The newly created component instance.</returns>
        Tcomp AddComponent<Tcomp>() where Tcomp : Component;
    }

    /// <summary>
    /// A cached wrapper around a Unity <see cref="Transform"/> that stores position, rotation, and scale locally
    /// for efficient access, while keeping them synchronized with the underlying Unity object.
    ///
    /// This wrapper allows systems to access and modify transform data without repeated engine calls,
    /// and provides an abstraction layer suitable for multi-threaded or data-oriented code.
    ///
    /// The associated <see cref="GameObject"/> is automatically destroyed when this instance is disposed.
    /// </summary>
    public class CachedTransform : ITransform
    {
        private readonly GameObject m_GameObject;
        private ITransform m_Parent;

        private Vector3 m_CachedPosition;
        private Quaternion m_CachedRotation;
        private Vector3 m_CachedScale;

        /// <summary>
        /// Gets the underlying Unity <see cref="Transform"/> instance associated with this cached transform.
        /// Use this property only when direct Unity API access is required.
        /// </summary>
        public virtual Transform Self => m_GameObject.transform;

        /// <summary>
        /// Initializes a new <see cref="CachedTransform"/> that wraps the specified <see cref="GameObject"/>.
        /// </summary>
        /// <param name="gameObject">The GameObject to wrap and cache transform data from.</param>
        public CachedTransform(GameObject gameObject)
        {
            if (gameObject == null)
            {
                Debug.LogError("CachedTransform: GameObject is null.");
                return;
            }

            m_GameObject = gameObject;
            var t = m_GameObject.transform;
            m_CachedPosition = t.position;
            m_CachedRotation = t.rotation;
            m_CachedScale = t.localScale;
        }

        /// <summary>
        /// Finalizer ensures cleanup if <see cref="Dispose"/> was not called manually.
        /// </summary>
        ~CachedTransform()
        {
            Dispose();
        }

        /// <summary>
        /// Creates a new <see cref="CachedTransform"/> by instantiating a new <see cref="GameObject"/> with the given name.
        /// </summary>
        /// <param name="name">The name of the new GameObject.</param>
        /// <returns>A new <see cref="CachedTransform"/> instance.</returns>
        public static CachedTransform Create(string name)
        {
            var gameObject = new GameObject(name);
            return new CachedTransform(gameObject);
        }

        /// <summary>
        /// Creates a new <see cref="CachedTransform"/> with a new <see cref="GameObject"/> that is immediately parented.
        /// </summary>
        /// <param name="name">The name of the new GameObject.</param>
        /// <param name="parent">The parent transform to attach to.</param>
        /// <returns>A new <see cref="CachedTransform"/> instance.</returns>
        public static CachedTransform Create(string name, ITransform parent)
        {
            var gameObject = new GameObject(name);
            var t = gameObject.transform;
            if (parent != null)
                t.SetParent(parent.Self);
            return new CachedTransform(gameObject) { m_Parent = parent };
        }

        /// <inheritdoc/>
        public virtual Vector3 Position
        {
            get => m_CachedPosition;
            set
            {
                if (m_CachedPosition != value)
                {
                    m_CachedPosition = value;
                    Self.position = value;
                }
            }
        }

        /// <inheritdoc/>
        public virtual Quaternion Rotation
        {
            get => m_CachedRotation;
            set
            {
                if (m_CachedRotation != value)
                {
                    m_CachedRotation = value;
                    Self.rotation = value;
                }
            }
        }

        /// <inheritdoc/>
        public virtual Vector3 Scale
        {
            get => m_CachedScale;
            set
            {
                if (m_CachedScale != value)
                {
                    m_CachedScale = value;
                    Self.localScale = value;
                }
            }
        }

        /// <inheritdoc/>
        public virtual ITransform Parent
        {
            get => m_Parent;
            set
            {
                // Allow unparenting
                Self.SetParent(value?.Self);
                m_Parent = value;
            }
        }

        /// <inheritdoc/>
        public virtual void SetParent(ITransform parent, bool worldPositionStays = false)
        {
            Self.SetParent(parent?.Self, worldPositionStays);
            m_Parent = parent;
        }

        /// <inheritdoc/>
        public Tcomp AddComponent<Tcomp>() where Tcomp : Component
            => m_GameObject.AddComponent<Tcomp>();

        /// <summary>
        /// Updates the cached position, rotation, and scale from the underlying Unity transform.
        /// Call this if the transform was externally modified.
        /// </summary>
        public virtual void Refresh()
        {
            var t = Self;
            m_CachedPosition = t.position;
            m_CachedRotation = t.rotation;
            m_CachedScale = t.localScale;
        }

        /// <summary>
        /// Destroys the underlying GameObject and releases references.
        /// </summary>
        public virtual void Dispose()
        {
            if (m_GameObject != null)
            {
                UnityEngine.Object.Destroy(m_GameObject);
                GC.SuppressFinalize(this);
            }
        }
    }

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
        public override ITransform Parent
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
        public override void SetParent(ITransform parent, bool worldPositionStays = false)
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