using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.VolumeComponentUtils.Parameters.Helpers
{
    public class GradientKeyComparer : IEqualityComparer<GradientColorKey>, IEqualityComparer<GradientAlphaKey>
    {
        public bool Equals(GradientColorKey a, GradientColorKey b)
            => a.EqualsGradientColorKey(b);

        public int GetHashCode(GradientColorKey obj)
        {
            unchecked
            {
                int hash = obj.time.GetHashCode();
                hash = (hash * 397) ^ obj.color.GetHashCode();
                return hash;
            }
        }

        public bool Equals(GradientAlphaKey a, GradientAlphaKey b)
            => a.EqualsGradientAlphaKey(b);

        public int GetHashCode(GradientAlphaKey obj)
        {
            unchecked
            {
                int hash = obj.time.GetHashCode();
                hash = (hash * 397) ^ obj.alpha.GetHashCode();
                return hash;
            }
        }
    }
}