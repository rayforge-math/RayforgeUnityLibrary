using Rayforge.VolumeComponentUtils.Abstractions;
using System;
using UnityEngine;
using UnityEngine.Rendering;

using static Rayforge.VolumeComponentUtils.Parameters.Helpers.KeyframeExtensions;

namespace Rayforge.VolumeComponentUtils.Parameters
{
    [System.Serializable]
    public class ObservableAnimationCurveParameter : AnimationCurveParameter, IObservableParameter<AnimationCurve>, IEquatable<ObservableAnimationCurveParameter>
    {
        private int m_Cached;

        public event NotifyDelegate<AnimationCurve> OnValueChanged;

        public ObservableAnimationCurveParameter(AnimationCurve value, bool overrideState = false)
            : base(value, overrideState) { }

        public override AnimationCurve value
        {
            get => base.value;
            set
            {
                if (!base.value.keys.EqualsKeyframes(value.keys))
                {
                    base.value.CopyFrom(value);
                    NotifyObservers();
                }
            }
        }

        public override void SetValue(VolumeParameter parameter)
        {
            var oldValue = value.keys;
            base.SetValue(parameter);
            if (!value.keys.EqualsKeyframes(oldValue))
                NotifyObservers();
        }

        public void NotifyObservers()
        {
            OnValueChanged?.Invoke(this);
        }

        public bool Changed()
        {
            var hash = GetHashCode();
            if (hash != m_Cached)
            {
                m_Cached = hash;
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            if (value == null || value.keys == null || value.keys.Length == 0)
                return 0;

            unchecked
            {
                int hash = 5381;

                foreach (var key in value.keys)
                {
                    hash = (hash * 33) ^ key.time.GetHashCode();
                    hash = (hash * 33) ^ key.value.GetHashCode();
                    hash = (hash * 33) ^ key.inTangent.GetHashCode();
                    hash = (hash * 33) ^ key.outTangent.GetHashCode();
                    hash = (hash * 33) ^ key.weightedMode.GetHashCode();
                    hash = (hash * 33) ^ key.inWeight.GetHashCode();
                    hash = (hash * 33) ^ key.outWeight.GetHashCode();
                }

                return hash;
            }
        }

        public bool Equals(ObservableAnimationCurveParameter other)
            => value.keys.EqualsKeyframes(other.value.keys);
    }
}