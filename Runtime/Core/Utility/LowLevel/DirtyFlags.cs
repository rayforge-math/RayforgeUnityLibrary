namespace Rayforge.Utility.LowLevel
{
    /// <summary>
    /// Lightweight dirty-flag container built on top of <see cref="BitField"/>.
    /// </summary>
    /// <remarks>
    /// Each bit represents a specific aspect of state that has changed and needs
    /// to be recomputed or updated. This allows fine-grained invalidation instead
    /// of relying on a single boolean dirty flag.
    /// 
    /// Typical use cases include render state updates, resource reallocation,
    /// parameter changes, or cache invalidation.
    /// </remarks>
    public struct DirtyFlags
    {
        /// <summary>
        /// Underlying bit field storing dirty state.
        /// </summary>
        private BitField m_Bits;

        /// <summary>
        /// Gets whether any dirty flag is currently set.
        /// </summary>
        public bool Any => m_Bits.Any;

        /// <summary>
        /// Marks the specified flags as dirty.
        /// </summary>
        /// <param name="mask">Bit mask identifying the dirty aspects.</param>
        public void MarkDirty(uint mask) => m_Bits.Set(mask);

        /// <summary>
        /// Marks all flags as dirty.
        /// </summary>
        /// <remarks>
        /// This is typically used when a full reset or reinitialization is required
        /// and all dependent state must be recomputed.
        /// </remarks>
        public void MarkAllDirty() => m_Bits.SetAll();

        /// <summary>
        /// Clears the specified dirty flags.
        /// </summary>
        /// <param name="mask">Bit mask identifying the flags to clear.</param>
        public void Clear(uint mask) => m_Bits.Unset(mask);

        /// <summary>
        /// Clears all dirty flags.
        /// </summary>
        public void ClearAll() => m_Bits.Reset();

        /// <summary>
        /// Determines whether any of the specified flags are marked as dirty.
        /// </summary>
        /// <param name="mask">Bit mask identifying the flags to test.</param>
        /// <returns>
        /// <c>true</c> if any of the specified flags are dirty; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDirtyAny(uint mask) => m_Bits.ContainsAny(mask);

        /// <summary>
        /// Determines whether all of the specified flags are marked as dirty.
        /// </summary>
        /// <param name="mask">Bit mask identifying the flags to test.</param>
        /// <returns>
        /// <c>true</c> if all of the specified flags are dirty; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDirty(uint mask) => m_Bits.Contains(mask);
    }
}