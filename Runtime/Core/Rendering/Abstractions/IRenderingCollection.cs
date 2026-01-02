using System;
using System.Collections.Generic;

namespace Rayforge.Rendering.Abstractions
{
	/// <summary>
	/// Represents a collection of renderable handles, providing both read-only access
	/// and span-based access for efficient operations on contiguous memory sections.
	/// </summary>
	/// <typeparam name="Thandle">Type of the handle stored in the collection (e.g., TextureHandle).</typeparam>
	public interface IRenderingCollection<Thandle>
	{
		/// <summary>
		/// Gets a read-only view of the handles in the collection.
		/// <para>
		/// Use this when you need standard collection semantics such as enumeration,
		/// LINQ queries, or compatibility with APIs expecting <see cref="IReadOnlyList{T}"/>.
		/// </para>
		/// </summary>
		IReadOnlyList<Thandle> Handles { get; }

		/// <summary>
		/// Provides a <see cref="ReadOnlySpan{T}"/> over a subrange of the collection.
		/// <para>
		/// This is useful for high-performance scenarios where you want to operate
		/// on a contiguous section of handles without allocating a new array and using stack based operations.
		/// </para>
		/// </summary>
		/// <param name="index">
		/// Start index of the span. Must be within the bounds of <see cref="Handles"/>.
		/// </param>
		/// <param name="count">
		/// Number of elements in the span starting from <paramref name="index"/>.
		/// </param>
		/// <returns>A <see cref="ReadOnlySpan{T}"/> representing the requested subrange.</returns>
		ReadOnlySpan<Thandle> AsSpan(int index, int count);

		/// <summary>
		/// Provides a <see cref="ReadOnlySpan{T}"/> over the entire collection.
		/// <para>
		/// Use this when you need a contiguous view of all handles for performance-critical operations.
		/// </para>
		/// </summary>
		/// <returns>A <see cref="ReadOnlySpan{T}"/> representing all handles in the collection.</returns>
		ReadOnlySpan<Thandle> AsSpan();
	}
}