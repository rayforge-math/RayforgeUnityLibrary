using System;
using System.Collections.Generic;

using Rayforge.Infrastructure.Abstractions;

namespace Rayforge.Infrastructure.Collections
{
    /// <summary>
    /// Generic ping-pong buffer that manages two handles of type <typeparamref name="THandle"/>.
    /// Provides a minimal mechanism to alternate between two slots, and allows replacing the slots if needed.
    /// The roles of "first" and "second" are determined by the current internal index, 
    /// which changes each time <see cref="Swap"/> is called.
    /// </summary>
    /// <typeparam name="THandle">The type of handle stored in the buffer (usually a value type).</typeparam>
    public class PingPongBuffer<THandle> : IRenderingCollection<THandle>
    {
        protected readonly THandle[] m_Handles;
        private int m_CurrentIndex;

        /// <summary>
        /// Gets the handle currently considered the "first" slot.
        /// This is the slot returned as "current" until <see cref="Swap"/> is called.
        /// After swapping, this getter will return the other slot.
        /// </summary>
        public THandle First => m_CurrentIndex == 0 ? m_Handles[0] : m_Handles[1];

        /// <summary>
        /// Gets the handle currently considered the "second" slot.
        /// This is the slot returned as the "alternate" or "previous" handle.
        /// After <see cref="Swap"/> is called, this getter will switch to the other slot.
        /// </summary>
        public THandle Second => m_CurrentIndex == 0 ? m_Handles[1] : m_Handles[0];

        public IReadOnlyList<THandle> Handles => m_Handles;

        /// <summary>
        /// Replaces the handle currently considered the "first" slot.
        /// </summary>
        /// <param name="handle">New handle to set as the first slot.</param>
        public void SetFirst(THandle handle)
        {
            if (m_CurrentIndex == 0) m_Handles[0] = handle;
            else m_Handles[1] = handle;
        }

        /// <summary>
        /// Replaces the handle currently considered the "second" slot.
        /// </summary>
        /// <param name="handle">New handle to set as the second slot.</param>
        public void SetSecond(THandle handle)
        {
            if (m_CurrentIndex == 0) m_Handles[1] = handle;
            else m_Handles[0] = handle;
        }

        /// <summary>
        /// Swaps the roles of the two slots.
        /// The slot previously returned by <see cref="First"/> becomes the new <see cref="Second"/>,
        /// and vice versa.
        /// </summary>
        public void Swap() => m_CurrentIndex = NextIndex(m_CurrentIndex);

        /// <summary>
        /// Constructs the <see cref="PingPongBuffer{THandle}"/> with two initial handles.
        /// The first handle is initially considered the "first" slot.
        /// </summary>
        /// <param name="handle0">Initial first handle (slot 0).</param>
        /// <param name="handle1">Initial second handle (slot 1).</param>
        public PingPongBuffer(THandle handle0, THandle handle1)
        {
            m_Handles = new THandle[2];

            m_Handles[0] = handle0;
            m_Handles[1] = handle1;
            m_CurrentIndex = 0;
        }

        /// <summary>
        /// Computes the index of the alternate slot in the ping-pong pair.
        /// </summary>
        /// <param name="index">Current index.</param>
        /// <returns>Index of the alternate slot (0 or 1).</returns>
        private static int NextIndex(int index) => (index + 1) & 1;

        public ReadOnlySpan<THandle> AsSpan()
            => m_Handles.AsSpan();

        public ReadOnlySpan<THandle> AsSpan(int index, int count)
        {
            index = Math.Clamp(index, 0, 1);
            count = Math.Clamp(count, 1, 2 - index);

            return m_Handles.AsSpan(index, count);
        }
    }
}