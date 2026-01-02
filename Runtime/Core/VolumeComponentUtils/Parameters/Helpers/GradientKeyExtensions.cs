using System.Linq;
using UnityEngine;

namespace Rayforge.VolumeComponentUtils.Parameters.Helpers
{
    public static class GradientKeyExtensions
    {
        public static bool EqualsGradientColorKey(this GradientColorKey a, GradientColorKey b, float epsilon = 0.0001f)
        {
            return
                a.color == b.color &&
                Mathf.Abs(a.time - b.time) < epsilon;
        }

        public static bool EqualsGradientColorKeys(this GradientColorKey[] a, GradientColorKey[] b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null || a.Length != b.Length)
                return false;

            return a.SequenceEqual(b, new GradientKeyComparer());
        }

        public static bool EqualsGradientAlphaKey(this GradientAlphaKey a, GradientAlphaKey b, float epsilon = 0.0001f)
        {
            return
                Mathf.Abs(a.alpha - b.alpha) < epsilon &&
                Mathf.Abs(a.time - b.time) < epsilon;
        }

        public static bool EqualsGradientAlphaKeys(this GradientAlphaKey[] a, GradientAlphaKey[] b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null || a.Length != b.Length)
                return false;

            return a.SequenceEqual(b, new GradientKeyComparer());
        }
    }
}