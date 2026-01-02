using Rayforge.VolumeComponentUtils.Abstractions;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Rayforge.VolumeComponentUtils.Parameters
{
    [System.Serializable]
    public class ObservableClampedIntParameter : ClampedIntParameter, IObservableParameter<int>
    {
        private int m_Cached;

        public event NotifyDelegate<int> OnValueChanged;

        public ObservableClampedIntParameter(int value, int min, int max, bool overrideState = false)
            : base(value, min, max, overrideState) { }

        public override int value
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
            if (!EqualityComparer<int>.Default.Equals(oldValue, value))
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