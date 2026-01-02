using System;
using UnityEngine;

using Rayforge.Caching.Abstractions;

namespace Rayforge.Caching.Transforms
{
    /// <summary>
    /// A cached wrapper around a Unity <see cref="Transform"/> that stores position, rotation, and scale locally
    /// for efficient access, while keeping them synchronized with the underlying Unity object.
    ///
    /// This wrapper allows systems to access and modify transform data without repeated engine calls,
    /// and provides an abstraction layer suitable for multi-threaded or data-oriented code.
    ///
    /// The associated <see cref="GameObject"/> is automatically destroyed when this instance is disposed.
    /// </summary>
    public class CachedTransform : ICachedTransform
    {
        private readonly GameObject m_GameObject;
        private ICachedTransform m_Parent;

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
        public static CachedTransform Create(string name, ICachedTransform parent)
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
        public virtual ICachedTransform Parent
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
        public virtual void SetParent(ICachedTransform parent, bool worldPositionStays = false)
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
}