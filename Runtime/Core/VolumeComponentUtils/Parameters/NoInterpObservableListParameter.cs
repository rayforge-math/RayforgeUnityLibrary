using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Rayforge.VolumeComponentUtils.Parameters.Helpers;

using static Rayforge.VolumeComponentUtils.Parameters.Helpers.ArrayExtensions;

namespace Rayforge.VolumeComponentUtils.Parameters
{
    [System.Serializable]
    public class NoInterpObservableListParameter<T> : NoInterpObservableParameter<ArrayWrapper<T>>
        where T : struct, IEquatable<T>
    {
        private readonly IEqualityComparer<T> m_Comparer;
        private int m_Hash;
        private int m_Cache;

        public NoInterpObservableListParameter(T[] value = null, IEqualityComparer<T> comparer = null, bool overrideState = false)
            : base(new ArrayWrapper<T>(value), overrideState)
        {
            m_Comparer = comparer ?? new ListParameterComparer<T>();
        }

        public override ArrayWrapper<T> value
        {
            get => m_Value;
            set
            {
                if (m_Value.ReferenceEquals(value)) return;

                var hash = value.array.ArrayToHash();
                if (m_Hash != hash)
                {
                    if (hash == 0)
                    {
                        m_Value = ArrayWrapper<T>.Empty();
                    }
                    else
                    {
                        m_Value.CopyFrom(value);
                    }
                    m_Hash = hash;
                    NotifyObservers();
                }
            }
        }

        public override void SetValue(VolumeParameter parameter)
        {
            value = parameter.GetValue<ArrayWrapper<T>>();
        }

        public override bool Changed()
        {
            if (m_Cache != m_Hash)
            {
                m_Cache = m_Hash;
                return true;
            }
            return false;
        }
    }
}