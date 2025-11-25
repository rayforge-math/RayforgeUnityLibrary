namespace Rayforge.ShaderExtensions.BrightFilter
{
    /// <summary>
    /// Brightness filter modes for threshold-based luminance filtering.
    /// </summary>
    public enum BrightFilterMode
    {
        /// <summary>Hard threshold; colors below the threshold are discarded.</summary>
        Hard = 0,

        /// <summary>Soft threshold; subtracts the threshold value from color.</summary>
        Soft = 1,

        /// <summary>Smooth threshold; smooth transition near threshold.</summary>
        Smooth = 2,

        /// <summary>Exponential threshold; smooth falloff with exponential curve.</summary>
        Exponential = 3
    }
}
