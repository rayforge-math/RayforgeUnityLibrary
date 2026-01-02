using Rayforge.VolumeComponentUtils.Abstractions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.VolumeComponentUtils.Parameters
{
    [System.Serializable]
    public class ObservableTextureParameter : TextureParameter, IObservableParameter<Texture>
    {
        private Texture m_Cached;

        public event NotifyDelegate<Texture> OnValueChanged;

        public ObservableTextureParameter(Texture value, bool overrideState = false)
            : base(value, overrideState) { }

        public override Texture value
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
            if (!EqualityComparer<Texture>.Default.Equals(oldValue, value))
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