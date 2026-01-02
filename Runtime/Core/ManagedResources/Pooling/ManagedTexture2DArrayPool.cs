using Rayforge.ManagedResources.Abstraction;
using Rayforge.ManagedResources.NativeMemory;

namespace Rayforge.ManagedResources.Pooling
{
    /// <summary>
    /// Managed pool for <see cref="ManagedTexture2DArray"/> objects.
    /// Provides default create/release functions.
    /// </summary>
    public sealed class ManagedTexture2DArrayPool : LeasedBufferPool<Texture2dArrayDescriptor, ManagedTexture2DArray>
    {
        /// <summary>
        /// Default constructor using standard factory methods for managed Texture2DArray objects.
        /// </summary>
        public ManagedTexture2DArrayPool()
            : base(
                createFunc: desc => new ManagedTexture2DArray(desc),
                releaseFunc: buffer => buffer.Release())
        { }

        /// <summary>
        /// Constructor allowing custom create/release functions.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer.</param>
        /// <param name="releaseFunc">Function to release a buffer permanently.</param>
        public ManagedTexture2DArrayPool(
            BufferCreateFunc createFunc,
            BufferReleaseFunc releaseFunc)
            : base(createFunc, releaseFunc)
        { }
    }
}