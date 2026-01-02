using Rayforge.VolumeComponentUtils.Abstractions;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

namespace Rayforge.VolumeComponentUtils.Parameters
{
    [System.Serializable]
    public class ObservableFloatParameter : FloatParameter, IObservableParameter<float>
    {
        private float m_Cached;

        public event NotifyDelegate<float> OnValueChanged;

        public ObservableFloatParameter(float value, bool overrideState = false)
            : base(value, overrideState) { }

        public override float value
        {
            get => base.value;
            set
            {
                if (!Mathf.Approximately(base.value, value))
                {
                    base.value = value;
                    NotifyObservers();
                }
            }
        }

        public override void SetValue(VolumeParameter parameter)
        {
            var oldValue = value;
            base.SetValue(parameter);
            if (!EqualityComparer<float>.Default.Equals(oldValue, value))
                NotifyObservers();
        }

        public void NotifyObservers()
        {
            OnValueChanged?.Invoke(this);
        }

        public bool Changed()
        {
            if (!Mathf.Approximately(m_Cached, base.value))
            {
                m_Cached = base.value;
                return true;
            }
            return false;
        }
    }
}