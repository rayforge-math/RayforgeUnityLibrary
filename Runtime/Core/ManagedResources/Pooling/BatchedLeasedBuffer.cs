using System;

namespace Rayforge.ManagedResources.Pooling
{
    /// <summary>
    /// Represents a leased buffer that supports batch validation within a pooled context.
    /// This type extends <see cref="LeasedBuffer{Tdesc,Tbuffer}"/> by providing a mechanism
    /// to verify whether the current buffer still fits within the batching constraints of the pool.
    /// </summary>
    /// <typeparam name="Tbuffer">The managed buffer type implementing <see cref="IPooledBuffer{Tdesc}"/>.</typeparam>
    public class BatchedLeasedBuffer<Tbuffer> : LeasedBuffer<Tbuffer>
    {
        private readonly BatchCheckFunc m_OnBatchCheck;
        private readonly RequestBatchedBufferFunc m_RequestNewBuffer;

        /// <summary>
        /// Delegate invoked to check whether a leased buffer still fits within the pool's current batching constraints.
        /// Used by batched pools to determine if a buffer can be reused for a given element count or requires reallocation.
        /// </summary>
        /// <typeparam name="Tbuffer">The managed buffer type.</typeparam>
        /// <param name="buffer">The underlying buffer to swap.</param>
        /// <param name="desiredCount">The desired number of elements to validate against the current batch allocation.</param>
        /// <returns>True if the buffer is still valid for the given batch size; otherwise, false.</returns>
        public delegate bool BatchCheckFunc(Tbuffer desc, int desiredCount);

        /// <summary>
        /// Delegate invoked to request a new batched buffer from the pool.
        /// Typically used when the current leased buffer is too small and needs resizing.
        /// </summary>
        /// <typeparam name="Tbuffer">The managed buffer type.</typeparam>
        /// <param name="buffer">The underlying buffer to swap.</param>
        /// <param name="desiredCount">The requested element count.</param>
        /// <returns>A new <see cref="BatchedLeasedBuffer{Tbuffer}"/> of the requested batch size.</returns>
        public delegate Tbuffer RequestBatchedBufferFunc(Tbuffer buffer, int desiredCount);

        /// <summary>
        /// Creates a new batched leased buffer.
        /// </summary>
        /// <param name="buffer">The underlying managed buffer.</param>
        /// <param name="onReturnHandle">
        /// Delegate invoked when the buffer is returned to the pool.
        /// </param>
        /// <param name="onBatchCheckHandle">
        /// Delegate invoked to check if the buffer still fits the pool's batching constraints for a given element count.
        /// </param>
        /// <param name="requestNewBufferFunc">
        /// Delegate invoked to request a new buffer of appropriate batch size if the current buffer is too small.
        /// </param>
        public BatchedLeasedBuffer(
            Tbuffer buffer,
            LeasedReturnFunc onReturnHandle,
            BatchCheckFunc onBatchCheckHandle,
            RequestBatchedBufferFunc requestNewBufferFunc)
            : base(buffer, onReturnHandle)
        {
            m_OnBatchCheck = onBatchCheckHandle ?? throw new ArgumentNullException(nameof(onBatchCheckHandle));
            m_RequestNewBuffer = requestNewBufferFunc ?? throw new ArgumentNullException(nameof(requestNewBufferFunc));
        }

        /// <summary>
        /// Checks whether the current buffer still fits within the batch size constraints.
        /// </summary>
        /// <param name="desiredCount">The desired element count to validate.</param>
        /// <returns>
        /// <c>true</c> if the buffer is still valid for the given batch size; <c>false</c> if a resize is needed.
        /// </returns>
        public bool EnsureBatchSize(int desiredCount)
            => m_OnBatchCheck?.Invoke(BufferHandle, desiredCount) ?? false;

        /// <summary>
        /// Replaces the current buffer with a new buffer of the requested batch size.
        /// The current buffer is returned to the pool before replacement.
        /// </summary>
        /// <param name="desiredCount">The desired element count for the new buffer.</param>
        /// <remarks>
        /// After calling this method, <see cref="BufferHandle"/> points to the newly acquired buffer.
        /// </remarks>
        public void Resize(int desiredCount)
        {
            m_BufferHandle = m_RequestNewBuffer.Invoke(BufferHandle, desiredCount);
        }
    }
}