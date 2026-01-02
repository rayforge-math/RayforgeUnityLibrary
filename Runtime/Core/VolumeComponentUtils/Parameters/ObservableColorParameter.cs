using Rayforge.VolumeComponentUtils.Abstractions;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

namespace Rayforge.VolumeComponentUtils.Parameters
{
    [System.Serializable]
    public class ObservableColorParameter : ColorParameter, IObservableParameter<Color>
    {
        private Color m_Cached;

        public event NotifyDelegate<Color> OnValueChanged;

        public ObservableColorParameter(Color value, bool overrideState = false)
            : base(value, overrideState) { }

        public override Color value
        {
            get => base.value;
            set
            {
                if (base.value != value)
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
            if (!EqualityComparer<Color>.Default.Equals(oldValue, value))
                NotifyObservers();
        }

        public void NotifyObservers()
        {
            OnValueChanged?.Invoke(this);
        }

        public bool Changed()
        {
            if (!m_Cached.Equals(base.value))
            {
                m_Cached = base.value;
                return true;
            }
            return false;
        }
    }
}