using System.Linq;
using UnityEngine;

namespace Rayforge.VolumeComponentUtils.Parameters.Helpers
{
    public static class KeyframeExtensions
    {
        public static bool EqualsKeyframe(this Keyframe a, Keyframe b, float epsilon = 0.0001f)
        {
            return
                Mathf.Abs(a.time - b.time) < epsilon &&
                Mathf.Abs(a.value - b.value) < epsilon &&
                Mathf.Abs(a.inTangent - b.inTangent) < epsilon &&
                Mathf.Abs(a.outTangent - b.outTangent) < epsilon &&
                Mathf.Abs(a.inWeight - b.inWeight) < epsilon &&
                Mathf.Abs(a.outWeight - b.outWeight) < epsilon &&
                a.weightedMode == b.weightedMode;
        }

        public static bool EqualsKeyframes(this Keyframe[] a, Keyframe[] b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null || a.Length != b.Length)
                return false;

            return a.SequenceEqual(b, new KeyframeComparer());
        }
    }
}