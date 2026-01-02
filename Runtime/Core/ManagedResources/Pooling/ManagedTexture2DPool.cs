using Rayforge.ManagedResources.NativeMemory;

namespace Rayforge.ManagedResources.Pooling
{
    /// <summary>
    /// Managed pool for <see cref="ManagedTexture2D"/> objects.
    /// Provides default create/release functions.
    /// </summary>
    public sealed class ManagedTexture2DPool : LeasedBufferPool<Texture2dDescriptor, ManagedTexture2D>
    {
        /// <summary>
        /// Default constructor using standard factory methods for managed Texture2D objects.
        /// </summary>
        public ManagedTexture2DPool()
            : base(
                createFunc: desc => new ManagedTexture2D(desc),
                releaseFunc: buffer => buffer.Release())
        { }

        /// <summary>
        /// Constructor allowing custom create/release functions.
        /// </summary>
        /// <param name="createFunc">Factory function to create a new buffer.</param>
        /// <param name="releaseFunc">Function to release a buffer permanently.</param>
        public ManagedTexture2DPool(
            BufferCreateFunc createFunc,
            BufferReleaseFunc releaseFunc)
            : base(createFunc, releaseFunc)
        { }
    }
}