namespace Rayforge.Core.ShaderExtensions.Filter
{
    /// <summary>
    /// Modes for color clamping or deviation damping.
    /// Mirrors the shader #define constants: CLAMP_NONE, CLAMP_MINMAX, CLAMP_VARIANCE, CLAMP_CLIPBOX, DAMP_STDDEV.
    /// </summary>
    public enum ColorClampMode
    {
        /// <summary>No clamping or damping.</summary>
        None = 0,

        /// <summary>Min/Max Clamp: clamps color to scaled min/max range of local neighborhood.</summary>
        MinMax = 1,

        /// <summary>Variance Clamp: clamps color to mean ± stdDev * scale.</summary>
        Variance = 2,

        /// <summary>ClipBox Clamp: luma-oriented clamp, UE-style.</summary>
        ClipBox = 3
    }

    /// <summary>
    /// Modes for color clamping types without 'None'.
    /// Mirrors the shader #define constants: CLAMP_NONE, CLAMP_MINMAX, CLAMP_VARIANCE, CLAMP_CLIPBOX, DAMP_STDDEV.
    /// </summary>
    public enum NoDisableColorClampMode
    {
        /// <summary>Min/Max Clamp: clamps color to scaled min/max range of local neighborhood.</summary>
        MinMax = ColorClampMode.MinMax,

        /// <summary>Variance Clamp: clamps color to mean ± stdDev * scale.</summary>
        Variance = ColorClampMode.Variance,

        /// <summary>ClipBox Clamp: luma-oriented clamp, UE-style.</summary>
        ClipBox = ColorClampMode.ClipBox
    }
}