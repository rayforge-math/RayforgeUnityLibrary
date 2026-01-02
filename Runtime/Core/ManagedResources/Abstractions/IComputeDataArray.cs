namespace Rayforge.ManagedResources.Abstractions
{
	/// <summary>
	/// Represents a CPU-side container that exposes backing array data for GPU upload.
	/// Used for data sets such as filter kernels or lookup tables.
	/// </summary>
	/// <typeparam name="Ttype">The unmanaged element type stored in the array.</typeparam>
	public interface IComputeDataArray<Ttype>
		where Ttype : unmanaged
	{
		/// <summary>
		/// Returns the raw array backing the data. This array is directly uploaded to GPU buffers.
		/// </summary>
		public Ttype[] RawData { get; }

		/// <summary>
		/// Returns count of elements in the raw array backing the data for GPU buffer upload.
		/// </summary>
		public int Count { get; }
	}
}