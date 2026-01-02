namespace Rayforge.Common.ColorSources
{
    /// <summary>
    /// Color sources that can be reliably queried via Unity API.
    /// </summary>
    public enum ColorSource
    {
        /// <summary>
        /// Manually assigned color.
        /// </summary>
        Manual,

        /// <summary>
        /// Unity's RenderSettings fog color.
        /// </summary>
        UnityFog,

        /// <summary>
        /// Unity's RenderSettings ambient sky color.
        /// </summary>
        AmbientSky,

        /// <summary>
        /// Unity's RenderSettings ambient equator color.
        /// </summary>
        AmbientEquator,

        /// <summary>
        /// Unity's RenderSettings ambient ground color.
        /// </summary>
        AmbientGround,

        /// <summary>
        /// Main directional light color.
        /// </summary>
        MainLight,
    }
}