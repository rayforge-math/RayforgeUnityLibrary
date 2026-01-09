namespace Rayforge.Core.ManagedResources.NativeMemory
{
    /// <summary>
    /// Interface providing access to the underlying internal buffer/resource.
    /// Implemented by managed buffers to expose their raw GPU/system resource.
    /// </summary>
    /// <typeparam name="TInternal">The internal buffer type (e.g., ComputeBuffer, NativeArray&lt;T&gt;).</typeparam>
    public interface IBufferInternal<TInternal>
    {
        /// <summary>
        /// The underlying internal GPU/system resource.
        /// </summary>
        public TInternal Buffer { get; }
    }
}