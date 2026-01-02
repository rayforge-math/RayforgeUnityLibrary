namespace Rayforge.ShaderExtensions.Filter
{
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
}
