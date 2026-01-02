using System;
using UnityEngine.Rendering;

namespace Rayforge.VolumeComponentUtils.Parameters
{
    [System.Serializable]
    public class ObservableClampedParameter<T> : ObservableParameter<T>
       where T : struct, IEquatable<T>
    {
        public readonly T min;
        public readonly T max;

        private readonly ClampFunc m_ClampFunc;

        public ObservableClampedParameter(T value, T min, T max, ClampFunc clamp, InterpFunc interp = null, bool overrideState = false)
            : base(value, interp, overrideState)
        {
            this.min = min;
            this.max = max;
            this.m_ClampFunc = clamp;
            this.value = value;
        }

        public override T value
        {
            get => m_Value;
            set
            {
                var clamped = m_ClampFunc.Invoke(value, min, max);
                if (!m_Value.Equals(clamped))
                {
                    m_Value = clamped;
                    NotifyObservers();
                }
            }
        }

        public override void SetValue(VolumeParameter parameter)
        {
            value = parameter.GetValue<T>();
        }
    }
}