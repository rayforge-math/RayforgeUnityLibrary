namespace Rayforge.ManagedResources.Abstractions
{
    /// <summary>
    /// Interface for descriptors that support batching.
    /// Implementing descriptors expose a <see cref="Count"/> property,
    /// which can be used by a buffer pool to compute batch-aligned allocations.
    /// </summary>
    public interface IBatchingDescriptor
    {
        /// <summary>
        /// The number of elements requested or represented by this descriptor.
        /// Used by buffer pools to round up to batch sizes if necessary.
        /// </summary>
        public int Count { get; set; }
    }
}