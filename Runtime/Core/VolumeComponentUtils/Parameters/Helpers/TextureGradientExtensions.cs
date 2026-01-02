using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.VolumeComponentUtils.Parameters.Helpers
{
    public static class TextureGradientExtensions
    {
        public static void CopyFromTextureGradient(this Gradient gradient, TextureGradient other)
        {
            gradient.mode = other.mode;
            gradient.colorSpace = other.colorSpace;

            gradient.colorKeys = other.colorKeys.ToArray();
            gradient.alphaKeys = other.alphaKeys.ToArray();
            /*
            if (gradient.colorKeys.Length != other.colorKeys.Length)
            {
                gradient.colorKeys = new GradientColorKey[other.colorKeys.Length];
            }
            if (gradient.alphaKeys.Length != other.alphaKeys.Length)
            {
                gradient.alphaKeys = new GradientAlphaKey[other.alphaKeys.Length];
            }
            Array.Copy(other.colorKeys, gradient.colorKeys, gradient.colorKeys.Length);
            Array.Copy(other.alphaKeys, gradient.alphaKeys, gradient.alphaKeys.Length);
            */
        }

        public static void CopyFromTextureGradient(this TextureGradient gradient, TextureGradient other)
            => gradient.SetKeys(other.colorKeys, other.alphaKeys, other.mode, other.colorSpace);

        public static bool EqualsTextureGradient(this Gradient gradient, TextureGradient other)
        {
            return
                gradient.colorKeys.EqualsGradientColorKeys(other.colorKeys) &&
                gradient.alphaKeys.EqualsGradientAlphaKeys(other.alphaKeys) &&
                gradient.mode == other.mode &&
                gradient.colorSpace == other.colorSpace;
        }

        public static bool EqualsTextureGradient(this TextureGradient gradient, TextureGradient other)
        {
            return
                gradient.colorKeys.EqualsGradientColorKeys(other.colorKeys) &&
                gradient.alphaKeys.EqualsGradientAlphaKeys(other.alphaKeys) &&
                gradient.mode == other.mode &&
                gradient.colorSpace == other.colorSpace;
        }

        public static int ToHashCode(this TextureGradient value)
        {
            if (value == null || value.colorKeys == null || value.colorKeys.Length == 0 || value.alphaKeys == null || value.alphaKeys.Length == 0)
                return 0;

            unchecked
            {
                int hash = 5381;

                foreach (var key in value.colorKeys)
                {
                    hash = (hash * 33) ^ key.color.GetHashCode();
                    hash = (hash * 33) ^ key.time.GetHashCode();
                }

                foreach (var key in value.alphaKeys)
                {
                    hash = (hash * 33) ^ key.alpha.GetHashCode();
                    hash = (hash * 33) ^ key.time.GetHashCode();
                }

                hash = (hash * 33) ^ value.mode.GetHashCode();
                hash = (hash * 33) ^ value.colorSpace.GetHashCode();

                return hash;
            }
        }
    }
}