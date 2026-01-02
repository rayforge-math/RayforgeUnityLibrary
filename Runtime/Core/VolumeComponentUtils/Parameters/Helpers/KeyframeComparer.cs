using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.VolumeComponentUtils.Parameters.Helpers
{
    public class KeyframeComparer : IEqualityComparer<Keyframe>
    {
        public bool Equals(Keyframe a, Keyframe b)
            => a.EqualsKeyframe(b);

        public int GetHashCode(Keyframe obj)
        {
            unchecked
            {
                int hash = obj.time.GetHashCode();
                hash = (hash * 397) ^ obj.value.GetHashCode();
                hash = (hash * 397) ^ obj.inTangent.GetHashCode();
                hash = (hash * 397) ^ obj.outTangent.GetHashCode();
                hash = (hash * 397) ^ obj.weightedMode.GetHashCode();
                hash = (hash * 397) ^ obj.inWeight.GetHashCode();
                hash = (hash * 397) ^ obj.outWeight.GetHashCode();
                return hash;
            }
        }
    }
}