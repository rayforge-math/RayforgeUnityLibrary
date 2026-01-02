using Rayforge.VolumeComponentUtils.Abstractions;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Rayforge.VolumeComponentUtils.Parameters
{
    [System.Serializable]
    public class ObservableBoolParameter : BoolParameter, IObservableParameter<bool>
    {
        private bool m_Cached;

        public event NotifyDelegate<bool> OnValueChanged;

        public ObservableBoolParameter(bool value, bool overrideState = false)
            : base(value, overrideState) { }

        public override bool value
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
            if (!EqualityComparer<bool>.Default.Equals(oldValue, value))
                NotifyObservers();
        }

        public void NotifyObservers()
        {
            OnValueChanged?.Invoke(this);
        }

        public bool Changed()
        {
            if (m_Cached != base.value)
            {
                m_Cached = base.value;
                return true;
            }
            return false;
        }
    }
}