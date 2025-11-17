using UnityEngine;

/// <summary>
/// Contains common shader-related utilities, helper functions, enums, and include definitions
/// used across Rayforge rendering effects and post-processing shaders.
/// All types in this namespace are intended to assist with shader operations, 
/// texture manipulations, and GPU-side computations.
/// </summary>
namespace Rayforge.ShaderExtensions.Common
{
    /// <summary>
    /// Specifies the different modes for mixing colors in shaders or post-processing operations.
    /// </summary>
    public enum ColorMixOption
    {
        /// <summary>No mixing is applied; the original color is retained.</summary>
        None = 0,

        /// <summary>Multiplies the base color with the mix color.</summary>
        Multiply = 1,

        /// <summary>Scales the mix color based on the luminance of the base color.</summary>
        Luminance = 2,

        /// <summary>Adds the mix color to the base color.</summary>
        Additive = 3
    }

    /// <summary>
    /// Same as <see cref="ColorMixOption"/>, but excludes the 'None' option to prevent disabling color mixing.
    /// Useful in contexts where a color mix is always required.
    /// </summary>
    public enum NoDisableColorMixOption
    {
        /// <summary>Multiplies the base color with the mix color.</summary>
        Multiply = ColorMixOption.Multiply,

        /// <summary>Scales the mix color based on the luminance of the base color.</summary>
        Luminance = ColorMixOption.Luminance,

        /// <summary>Adds the mix color to the base color.</summary>
        Additive = ColorMixOption.Additive
    }

    /// <summary>
    /// Defines the types of blur that can be applied to a texture or render target.
    /// </summary>
    public enum BlurType
    {
        /// <summary>No blur is applied.</summary>
        None = 0,

        /// <summary>Simple box blur (average of neighboring pixels).</summary>
        Box = 1,

        /// <summary>Gaussian blur for smooth falloff.</summary>
        Gaussian = 2,

        /// <summary>Tent blur (linear falloff kernel).</summary>
        Tent = 3,

        /// <summary>Kawase blur (iterative multi-pass blur for performance).</summary>
        Kawase = 4
    }

    /// <summary>
    /// Directional blur types; excludes Kawase which is typically 2D.
    /// </summary>
    public enum BlurTypeDirectional
    {
        None = BlurType.None,
        Box = BlurType.Box,
        Gaussian = BlurType.Gaussian,
        Tent = BlurType.Tent
    }

    /// <summary>
    /// 2D blur types; includes Kawase for full 2D passes.
    /// </summary>
    public enum BlurType2D
    {
        None = BlurType.None,
        Box = BlurType.Box,
        Gaussian = BlurType.Gaussian,
        Tent = BlurType.Tent,
        Kawase = BlurType.Kawase
    }

    /// <summary>
    /// Blur types excluding the 'None' option; used when a blur must always be applied.
    /// </summary>
    public enum NoDisableBlurType
    {
        Box = BlurType.Box,
        Gaussian = BlurType.Gaussian,
        Tent = BlurType.Tent,
        Kawase = BlurType.Kawase
    }

    /// <summary>
    /// Directional blur types without 'None'.</summary>
    public enum NoDisableBlurTypeDirectional
    {
        Box = NoDisableBlurType.Box,
        Gaussian = NoDisableBlurType.Gaussian,
        Tent = NoDisableBlurType.Tent
    }

    /// <summary>
    /// 2D blur types without 'None'.</summary>
    public enum NoDisableBlurType2D
    {
        Box = NoDisableBlurType.Box,
        Gaussian = NoDisableBlurType.Gaussian,
        Tent = NoDisableBlurType.Tent,
        Kawase = NoDisableBlurType.Kawase
    }

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