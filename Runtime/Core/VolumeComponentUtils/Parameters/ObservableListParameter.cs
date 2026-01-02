using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
using Rayforge.VolumeComponentUtils.Abstractions;
using Rayforge.VolumeComponentUtils.Parameters.Helpers;

using static Rayforge.VolumeComponentUtils.Parameters.Helpers.ArrayExtensions;

namespace Rayforge.VolumeComponentUtils.Parameters
{
    [System.Serializable]
    public class ObservableListParameter<T> : ObservableParameter<ArrayWrapper<T>>
        where T : struct, IEquatable<T>, IInterpolatable<T>
    {
        private readonly IEqualityComparer<T> m_Comparer;
        private int m_Hash;
        private int m_Cache;

        public ObservableListParameter(T[] value = null, IEqualityComparer<T> comparer = null, bool overrideState = false)
            : base(new ArrayWrapper<T>(value), ArrayInterp, overrideState)
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

        private static ArrayWrapper<T> ArrayInterp(ArrayWrapper<T> from, ArrayWrapper<T> to, float t)
        {
            int fromLength = from.IsValid() ? from.Length : 0;
            int toLength = to.IsValid() ? to.Length : 0;

            int length = Mathf.Max(fromLength, toLength);
            if (length == 0) return new ArrayWrapper<T>(0);

            T[] result = new T[length];
            for (int i = 0; i < length; ++i)
            {
                T fromVal = i < fromLength ? from[i] : new();
                T toVal = i < toLength ? to[i] : new();

                result[i].Interp(fromVal, toVal, t);
            }
            return new ArrayWrapper<T>(result);
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