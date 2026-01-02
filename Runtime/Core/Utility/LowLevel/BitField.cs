namespace Rayforge.Utility.LowLevel
{
    /// <summary>
    /// Lightweight bit field utility for managing boolean flags packed into a <see cref="uint"/>.
    /// </summary>
    /// <remarks>
    /// This struct is intended for fast flag checks and modifications in performance-critical code
    /// (e.g. render features, passes, or state tracking).
    /// 
    /// The bit field does not perform validation on the provided masks; it is the caller's
    /// responsibility to ensure masks are valid and non-overlapping where required.
    /// </remarks>
    public struct BitField
    {
        /// <summary>
        /// Internal bit storage.
        /// </summary>
        private uint m_Value;

        /// <summary>
        /// Gets whether at least one bit is set in the field.
        /// </summary>
        public bool Any => m_Value != 0;

        /// <summary>
        /// Sets all bits specified by the given mask.
        /// </summary>
        /// <param name="mask">Bit mask indicating which bits to set.</param>
        public void Set(uint mask) => m_Value |= mask;

        /// <summary>
        /// Clears all bits specified by the given mask.
        /// </summary>
        /// <param name="mask">Bit mask indicating which bits to clear.</param>
        public void Unset(uint mask) => m_Value &= ~mask;

        /// <summary>
        /// Sets all bits in the bit field.
        /// </summary>
        public void SetAll() => m_Value = ~0u;

        /// <summary>
        /// Clears all bits in the bit field.
        /// </summary>
        public void Reset() => m_Value = 0;

        /// <summary>
        /// Determines whether any bit from the given mask is set.
        /// </summary>
        /// <param name="mask">Bit mask to test.</param>
        /// <returns>
        /// <c>true</c> if at least one bit in <paramref name="mask"/> is set;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool IsSetAny(uint mask) => (m_Value & mask) != 0;

        /// <summary>
        /// Determines whether any bit from the given mask is set.
        /// </summary>
        /// <remarks>
        /// Alias for <see cref="IsSetAny(uint)"/> provided for semantic clarity.
        /// </remarks>
        public bool ContainsAny(uint mask) => IsSetAny(mask);

        /// <summary>
        /// Determines whether all bits from the given mask are set.
        /// </summary>
        /// <param name="mask">Bit mask to test.</param>
        /// <returns>
        /// <c>true</c> if all bits in <paramref name="mask"/> are set;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool IsSet(uint mask) => (m_Value & mask) == mask;

        /// <summary>
        /// Determines whether all bits from the given mask are set.
        /// </summary>
        /// <remarks>
        /// Alias for <see cref="IsSet(uint)"/> provided for semantic clarity.
        /// </remarks>
        public bool Contains(uint mask) => IsSet(mask);
    }
}