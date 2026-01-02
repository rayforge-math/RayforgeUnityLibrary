using Rayforge.VolumeComponentUtils.Abstractions;
using System;
using UnityEngine.Rendering;

using static Rayforge.VolumeComponentUtils.Parameters.Helpers.TextureGradientExtensions;

namespace Rayforge.VolumeComponentUtils.Parameters
{
    [System.Serializable]
    public class ObservableTextureGradientParameter : TextureGradientParameter, IObservableParameter<TextureGradient>, IEquatable<ObservableTextureGradientParameter>
    {
        private int m_Cache;

        public event NotifyDelegate<TextureGradient> OnValueChanged;

        private int m_Hash;

        public ObservableTextureGradientParameter(TextureGradient value, bool overrideState = false)
            : base(value, overrideState)
        { }

        public override TextureGradient value
        {
            get => base.value;
            set
            {
                var hash = value.ToHashCode();
                if (m_Hash != hash)
                {
                    m_Hash = hash;
                    base.value.CopyFromTextureGradient(value);
                    NotifyObservers();
                }
            }
        }

        public override void SetValue(VolumeParameter parameter)
        {
            var gradient = ((TextureGradientParameter)parameter).value;
            value = gradient;
        }

        public void NotifyObservers()
        {
            OnValueChanged?.Invoke(this);
        }

        public bool Changed()
        {
            if (m_Cache != m_Hash)
            {
                m_Cache = m_Hash;
                return true;
            }
            return false;
        }

        public bool Equals(ObservableTextureGradientParameter other)
            => value.EqualsTextureGradient(other.value);
    }
}