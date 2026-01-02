namespace Rayforge.Rendering.Collections
{
    /// <summary>
    /// Manages a pair of persistent render targets (history handles) for frame-over-frame operations.
    /// One handle represents the current target (write), the other holds the previous frame's data (read).
    /// Suitable for temporal effects like reprojection, motion blur, or any frame-history dependent process.
    /// </summary>
    public class HistoryHandles<Thandle> : PingPongBuffer<Thandle>
    {
        /// <summary>
        /// Gets the handle used as the current frame's target (write).
        /// </summary>
        public Thandle Target => First;

        /// <summary>
        /// Gets the handle containing the previous frame's data (read).
        /// </summary>
        public Thandle History => Second;

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryHandles"/> struct.
        /// </summary>
        /// <param name="initial0">Initial first handle (current).</param>
        /// <param name="initial1">Initial second handle (history).</param>
        public HistoryHandles(Thandle initial0, Thandle initial1)
            : base(initial0, initial1)
        { }
    }
}