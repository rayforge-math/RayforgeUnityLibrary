using Rayforge.Core.Diagnostics;
using System;

namespace Rayforge.Core.ManagedResources.Pooling
{
    /// <summary>
    /// Base class for all leased buffer wrappers. Handles lifetime management,
    /// validation, and return-to-pool semantics.
    /// </summary>
    public class LeasedBuffer<TBuffer>
    {
        protected TBuffer m_BufferHandle;
        protected bool m_Valid;
        private readonly LeasedReturnFunc m_OnReturn;

        /// <summary>
        /// Delegate invoked when a leased buffer is returned to the pool.
        /// The delegate should handle marking the buffer as free and any custom logic.
        /// Returns true if the buffer was successfully returned; false if the buffer was not recognized or could not be returned.
        /// </summary>
        /// <typeparam name="TBuffer">The managed buffer type.</typeparam>
        /// <param name="buffer">The buffer being returned to the pool.</param>
        public delegate void LeasedReturnFunc(TBuffer buffer);

        /// <summary>
        /// The underlying pooled buffer instance.
        /// Throws if accessed after return.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if accessed after the buffer has been returned to the pool.</exception>
        public TBuffer BufferHandle
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

        /// <summary>
        /// Initializes a new leased buffer.
        /// </summary>
        /// <param name="buffer">The underlying managed buffer.</param>
        /// <param name="onReturnHandle">
        /// Delegate invoked when the buffer is returned to the pool.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="buffer"/> or <paramref name="onReturnHandle"/> is null.
        /// </exception>
        public LeasedBuffer(TBuffer buffer, LeasedReturnFunc onReturnHandle)
        {
            m_BufferHandle = buffer ?? 
                throw new ArgumentNullException(nameof(buffer));
            m_OnReturn = onReturnHandle ?? 
                throw new ArgumentNullException(nameof(onReturnHandle));

            m_Valid = true;
        }

        /// <summary>
        /// Returns the buffer to the pool and invalidates this lease.
        /// A lease may only be returned once.
        /// </summary>
        public virtual void Return()
        {
            Assertions.IsTrue(m_Valid, "Attempted to return a buffer lease that is already invalid.");

            if (!m_Valid)
                return;

            m_Valid = false;
            m_OnReturn.Invoke(m_BufferHandle);
        }
    }
}