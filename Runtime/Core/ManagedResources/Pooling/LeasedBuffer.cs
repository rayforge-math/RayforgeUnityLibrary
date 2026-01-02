using System;

namespace Rayforge.ManagedResources.Pooling
{
    /// <summary>
    /// Base class for all leased buffer wrappers. Handles lifetime management,
    /// validation, and return-to-pool semantics.
    /// </summary>
    public class LeasedBuffer<Tbuffer>
    {
        protected Tbuffer m_BufferHandle;
        protected bool m_Valid;
        private readonly LeasedReturnFunc m_OnReturn;

        /// <summary>
        /// Delegate invoked when a leased buffer is returned to the pool.
        /// The delegate should handle marking the buffer as free and any custom logic.
        /// Returns true if the buffer was successfully returned; false if the buffer was not recognized or could not be returned.
        /// </summary>
        /// <typeparam name="Tbuffer">The managed buffer type.</typeparam>
        /// <param name="buffer">The buffer being returned to the pool.</param>
        /// <returns>True if the buffer was successfully returned; otherwise, false.</returns>
        public delegate bool LeasedReturnFunc(Tbuffer buffer);

        /// <summary>
        /// The underlying pooled buffer instance.
        /// Throws if accessed after return.
        /// </summary>
        public Tbuffer BufferHandle
        {
            get
            {
                if (!m_Valid)
                    throw new InvalidOperationException("Cannot access buffer after it has been returned to the pool.");
                return m_BufferHandle;
            }
        }

        /// <summary>
        /// Indicates whether the lease is still active.
        /// </summary>
        public bool IsValid => m_Valid;

        public LeasedBuffer(Tbuffer buffer, LeasedReturnFunc onReturnHandle)
        {
            m_BufferHandle = buffer ?? throw new ArgumentNullException(nameof(buffer));
            m_OnReturn = onReturnHandle ?? throw new ArgumentNullException(nameof(onReturnHandle));
            m_Valid = true;
        }

        /// <summary>
        /// Returns the buffer to the pool and invalidates this lease.
        /// </summary>
        /// <returns>True if successfully returned; otherwise false.</returns>
        public virtual bool Return()
        {
            if (!m_Valid)
                return false;

            m_Valid = false;
            return m_OnReturn.Invoke(m_BufferHandle);
        }
    }
}