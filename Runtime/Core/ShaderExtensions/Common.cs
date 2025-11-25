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
}