namespace Rayforge.ManagedResources.Abstractions
{
    /// <summary>
    /// Represents a CPU-side container that exposes backing single element array data for GPU upload.
    /// Used for data sets such as constant buffers, etc.
    /// </summary>
    /// <typeparam name="Ttype">The unmanaged element type stored in the array.</typeparam>
    public interface IComputeData<Ttype>
        where Ttype : unmanaged
    {
        /// <summary>
        /// Returns the raw array backing the data. This array is directly uploaded to GPU buffers.
        /// </summary>
        public Ttype RawData { get; }
    }
}