namespace Rayforge.ManagedResources.Abstractions
{
    /// <summary>
    /// Represents a CPU-side container that exposes backing single-element data
    /// for GPU upload.
    ///
    /// Typically used for constant buffer style data uploads
    /// (see <see cref="UnityEngine.ComputeBuffer"/> with
    /// <see cref="UnityEngine.ComputeBufferType.Constant"/>).
    /// </summary>
    /// <typeparam name="Ttype">The unmanaged element type.</typeparam>
    public interface IComputeData<Ttype>
        where Ttype : unmanaged
    {
        /// <summary>
        /// Returns the raw data element that is uploaded to the GPU.
        /// Intended primarily for single-element constant buffer bindings.
        /// </summary>
        Ttype RawData { get; }
    }
}
