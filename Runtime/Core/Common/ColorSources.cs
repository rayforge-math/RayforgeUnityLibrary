using UnityEngine;

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

    /// <summary>
    /// Resolves colors from ColorSource enum using Unity API.
    /// </summary>
    public static class ColorSourceResolver
    {
        /// <summary>
        /// Returns the color for the given ColorSource.
        /// </summary>
        /// <param name="source">The color source to resolve.</param>
        /// <param name="manualColor">Optional manual color (used if source is Manual).</param>
        /// <returns>The resolved color.</returns>
        public static Color GetColor(ColorSource source, Color manualColor = default)
        {
            switch (source)
            {
                case ColorSource.Manual:
                    return manualColor;
                case ColorSource.UnityFog:
                    return RenderSettings.fogColor;
                case ColorSource.AmbientSky:
                    return RenderSettings.ambientSkyColor;
                case ColorSource.AmbientEquator:
                    return RenderSettings.ambientEquatorColor;
                case ColorSource.AmbientGround:
                    return RenderSettings.ambientGroundColor;
                case ColorSource.MainLight:
                    {
                        Light mainLight = RenderSettings.sun;
                        if (mainLight != null)
                            return mainLight.color * mainLight.intensity;
                        else
                            return Color.white; // fallback if no directional light assigned
                    }
                default:
                    return Color.white;
            }
        }
    }
}