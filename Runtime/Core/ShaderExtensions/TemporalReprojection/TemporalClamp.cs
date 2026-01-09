using Rayforge.Core.ShaderExtensions.Filter;

namespace Rayforge.Core.ShaderExtensions.TemporalReprojection
{
    /// <summary>
    /// Defines the color clamping modes used in temporal anti-aliasing (TAA) shaders.
    /// These modes control how the current frame color is clamped based on history neighborhoods
    /// to reduce ghosting and temporal artifacts.
    /// </summary>
    public enum TemporalClamp : int
    {
        /// <summary>
        /// No clamping is performed; the current color is blended directly with the history.
        /// </summary>
        None = ColorClampMode.None,

        /// <summary>
        /// Performs a simple min/max clamp of the current color against the 3×3 neighborhood
        /// of the current frame or reprojected history.
        /// Limits extreme deviations without considering statistical variance.
        /// </summary>
        MinMax = ColorClampMode.MinMax,

        /// <summary>
        /// Performs variance-based clamping using the mean and standard deviation
        /// of the 3×3 neighborhood of the current frame.
        /// Simple statistical clamp: historyColor ∈ [mean - stdDev*scale, mean + stdDev*scale].
        /// </summary>
        Variance = ColorClampMode.Variance,

        /// <summary>
        /// Performs luma-oriented clip-box clamping along the principal luma direction,
        /// roughly following Unreal Engine's approach.
        /// See: https://de45xmedrsdbp.cloudfront.net/Resources/files/TemporalAA_small-59732822.pdf#page=34
        /// </summary>
        ClipBox = ColorClampMode.ClipBox
    }
}