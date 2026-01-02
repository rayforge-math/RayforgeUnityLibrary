using Rayforge.VolumeComponentUtils.Abstractions;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Rayforge.VolumeComponentUtils.Parameters
{
    public abstract class ObservableBase<T> : VolumeParameter<T>, IObservableParameter<T>
        where T : struct
    {
        public delegate T InterpFunc(T from, T to, float t);
        public delegate T ClampFunc(T value, T min, T max);

        private T m_Cached;

        public event NotifyDelegate<T> OnValueChanged;

        public ObservableBase(T value, bool overrideState = false)
            : base(value, overrideState) { }

        public override T value
        {
            get => base.value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(base.value, value))
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
            if (!EqualityComparer<T>.Default.Equals(oldValue, value))
                NotifyObservers();
        }

        public void NotifyObservers()
        {
            OnValueChanged?.Invoke(this);
        }

        public virtual bool Changed()
        {
            if (!EqualityComparer<T>.Default.Equals(m_Cached, value))
            {
                m_Cached = value;
                return true;
            }
            return false;
        }
    }
}