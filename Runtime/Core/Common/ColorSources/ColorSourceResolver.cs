using UnityEngine;

namespace Rayforge.Common.ColorSources
{
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