using System;
using UnityEngine;

namespace Rayforge.Caching.Abstractions
{
    /// <summary>
    /// Defines a lightweight abstraction layer over Unity's <see cref="Transform"/> component,
    /// providing cached access to position, rotation, scale, and parenting relationships.
    ///
    /// This interface allows safer use of transforms in systems where direct access to
    /// UnityEngine.Transform is undesirable — for example, in multi-threaded or data-oriented contexts.
    /// </summary>
    public interface ICachedTransform : IDisposable
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
        ICachedTransform Parent { get; set; }

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
        void SetParent(ICachedTransform parent, bool worldPositionStays = false);

        /// <summary>
        /// Adds a new Unity <see cref="Component"/> of type <typeparamref name="Tcomp"/> to the underlying GameObject.
        /// </summary>
        /// <typeparam name="Tcomp">The type of component to add.</typeparam>
        /// <returns>The newly created component instance.</returns>
        Tcomp AddComponent<Tcomp>() where Tcomp : Component;
    }
}