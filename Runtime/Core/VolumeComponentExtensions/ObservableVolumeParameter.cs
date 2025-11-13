using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.VolumeComponentExtensions
{
    public delegate T InterpFunc<T>(T from, T to, float t);
    public delegate T ClampFunc<T>(T value, T min, T max);
    public delegate void NotifyDelegate<T>(IObservableParameter<T> sender);

    public interface IObservableParameter<T>
    {
        public event NotifyDelegate<T> OnValueChanged;
        public void NotifyObservers();
        public bool Changed();
    }

    public abstract class ObservableBase<T> : VolumeParameter<T>, IObservableParameter<T>
        where T : struct
    {
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

        public bool Changed()
        {
            if(!EqualityComparer<T>.Default.Equals(m_Cached, value))
            {
                m_Cached = value;
                return true;
            }
            return false;
        }
    }

    [System.Serializable]
    public class NoInterpObservableParameter<T> : ObservableBase<T>
        where T : struct
    {
        public NoInterpObservableParameter(T value, bool overrideState = false)
            : base(value, overrideState) { }

        public override void Interp(T from, T to, float t)
        {
            value = t > 0f ? to : from;
        }
    }

    [System.Serializable]
    public class ObservableParameter<T> : ObservableBase<T>
        where T : struct
    {
        private readonly InterpFunc<T> m_InterpFunc;

        public ObservableParameter(T value, InterpFunc<T> interp, bool overrideState = false)
            : base(value, overrideState)
        {
            m_InterpFunc = interp;
        }

        public override void Interp(T from, T to, float t)
        {
            if (m_InterpFunc != null)
            {
                value = m_InterpFunc.Invoke(from, to, t);
            }
        }
    }

    [System.Serializable]
    public class ObservableClampedParameter<T> : ObservableParameter<T>
        where T : struct, IEquatable<T>
    {
        public readonly T min;
        public readonly T max;

        private readonly ClampFunc<T> m_ClampFunc;

        public ObservableClampedParameter(T value, T min, T max, ClampFunc<T> clamp, InterpFunc<T> interp = null, bool overrideState = false)
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

    public class ListParameterComparer<T> : IEqualityComparer<T>
        where T : IEquatable<T>
    {
        public bool Equals(T a, T b)
            => a.Equals(b);

        public int GetHashCode(T obj)
        {
            unchecked
            {
                return obj.GetHashCode();
            }
        }
    }

    public interface IInterpolatable<T>
    {
        public void Interp(T from, T to, float t);
    }

    [System.Serializable]
    public struct ArrayWrapper<T> : IEnumerable<T>, IEquatable<ArrayWrapper<T>>
        where T : struct
    {
        [SerializeField]
        private T[] array;

        public int Length => array.Length;
        public T this[int index] => array[index];

        public ArrayWrapper(T[] array)
        {
            this.array = array ?? Array.Empty<T>();
        }

        public ArrayWrapper(int length)
        {
            this.array = length > 0 ? new T[length] : Array.Empty<T>();
        }

        public bool IsValid()
            => array != null && array.Length > 0;

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Length; ++i)
                yield return array[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(ArrayWrapper<T> other)
        {
            if (ReferenceEquals(array, other.array)) return true;
            if (IsValid() != other.IsValid()) return false;
            if (!IsValid() && !other.IsValid()) return true;
            if (array.Length != other.Length) return false;

            return array.SequenceEqual(other);
        }

        public static ArrayWrapper<T> Empty()
            => new ArrayWrapper<T>(Array.Empty<T>());

        public void CopyFrom(ArrayWrapper<T> other)
        {
            if(other.IsValid())
            {
                if (!IsValid() || array.Length != other.Length)
                    array = new T[other.Length];
                Array.Copy(other.array, array, other.array.Length);
            }
            else
            {
                array = Array.Empty<T>();
            }
        }
    }

    [System.Serializable]
    public class ObservableListParameter<T> : ObservableParameter<ArrayWrapper<T>>
        where T : struct, IEquatable<T>, IInterpolatable<T>
    {
        private readonly IEqualityComparer<T> m_Comparer;

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
                if (m_Value.Equals(value)) return;
                if (!value.IsValid())
                {
                    m_Value = ArrayWrapper<T>.Empty();
                    NotifyObservers();
                }
                else if (!m_Value.SequenceEqual(value, m_Comparer))
                {
                    m_Value.CopyFrom(value);
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
            if(length == 0) return new ArrayWrapper<T>(0);

            T[] result = new T[length];
            for (int i = 0; i < length; ++i)
            {
                T fromVal = i < fromLength ? from[i] : new();
                T toVal = i < toLength ? to[i] : new();

                result[i].Interp(fromVal, toVal, t);
            }
            return new ArrayWrapper<T>(result);
        }
    }

    [System.Serializable]
    public class NoInterpObservableListParameter<T> : NoInterpObservableParameter<ArrayWrapper<T>>
        where T : struct, IEquatable<T>
    {
        private readonly IEqualityComparer<T> m_Comparer;

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
                if (m_Value.Equals(value)) return;
                if(!value.IsValid())
                {
                    m_Value = ArrayWrapper<T>.Empty();
                    NotifyObservers();
                }
                else if (!m_Value.SequenceEqual(value, m_Comparer))
                {
                    m_Value.CopyFrom(value);
                    NotifyObservers();
                }
            }
        }

        public override void SetValue(VolumeParameter parameter)
        {
            value = parameter.GetValue<ArrayWrapper<T>>();
        }
    }

    [System.Serializable]
    public class ObservableIntParameter : IntParameter, IObservableParameter<int>
    {
        private int m_Cached;

        public event NotifyDelegate<int> OnValueChanged;

        public ObservableIntParameter(int value, bool overrideState = false)
            : base(value, overrideState) { }

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
            if(m_Cached != base.value)
            {
                m_Cached = base.value;
                return true;
            }
            return false;
        }
    }

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

    [System.Serializable]
    public class ObservableClampedFloatParameter : ClampedFloatParameter, IObservableParameter<float>
    {
        private float m_Cached;

        public event NotifyDelegate<float> OnValueChanged;

        public ObservableClampedFloatParameter(float value, float min, float max, bool overrideState = false)
            : base(value, min, max, overrideState) { }

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

    public class KeyframeComparer : IEqualityComparer<Keyframe>
    {
        public bool Equals(Keyframe a, Keyframe b)
            => a.EqualsKeyframe(b);

        public int GetHashCode(Keyframe obj)
        {
            unchecked
            {
                int hash = obj.time.GetHashCode();
                hash = (hash * 397) ^ obj.value.GetHashCode();
                hash = (hash * 397) ^ obj.inTangent.GetHashCode();
                hash = (hash * 397) ^ obj.outTangent.GetHashCode();
                hash = (hash * 397) ^ obj.weightedMode.GetHashCode();
                hash = (hash * 397) ^ obj.inWeight.GetHashCode();
                hash = (hash * 397) ^ obj.outWeight.GetHashCode();
                return hash;
            }
        }
    }

    public static class KeyframeExtensions
    {
        public static bool EqualsKeyframe(this Keyframe a, Keyframe b, float epsilon = 0.0001f)
        {
            return
                Mathf.Abs(a.time - b.time) < epsilon &&
                Mathf.Abs(a.value - b.value) < epsilon &&
                Mathf.Abs(a.inTangent - b.inTangent) < epsilon &&
                Mathf.Abs(a.outTangent - b.outTangent) < epsilon &&
                Mathf.Abs(a.inWeight - b.inWeight) < epsilon &&
                Mathf.Abs(a.outWeight - b.outWeight) < epsilon &&
                a.weightedMode == b.weightedMode;
        }

        public static bool EqualsKeyframes(this Keyframe[] a, Keyframe[] b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null || a.Length != b.Length)
                return false;

            return a.SequenceEqual(b, new KeyframeComparer());
        }
    }

    [System.Serializable]
    public class ObservableAnimationCurveParameter : AnimationCurveParameter, IObservableParameter<AnimationCurve>, IEquatable<ObservableAnimationCurveParameter>
    {
        private int m_Cached;

        public event NotifyDelegate<AnimationCurve> OnValueChanged;

        public ObservableAnimationCurveParameter(AnimationCurve value, bool overrideState = false)
            : base(value, overrideState) { }

        public override AnimationCurve value
        {
            get => base.value;
            set
            {
                if (!base.value.keys.EqualsKeyframes(value.keys))
                {
                    base.value.CopyFrom(value);
                    NotifyObservers();
                }
            }
        }

        public override void SetValue(VolumeParameter parameter)
        {
            var oldValue = value.keys;
            base.SetValue(parameter);
            if (!value.keys.EqualsKeyframes(oldValue))
                NotifyObservers();
        }

        public void NotifyObservers()
        {
            OnValueChanged?.Invoke(this);
        }

        public bool Changed()
        {
            var hash = GetHashCode();
            if (hash != m_Cached)
            {
                m_Cached = hash;
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            if (value == null || value.keys == null || value.keys.Length == 0)
                return 0;

            unchecked
            {
                int hash = 5381;

                foreach (var key in value.keys)
                {
                    hash = (hash * 33) ^ key.time.GetHashCode();
                    hash = (hash * 33) ^ key.value.GetHashCode();
                    hash = (hash * 33) ^ key.inTangent.GetHashCode();
                    hash = (hash * 33) ^ key.outTangent.GetHashCode();
                    hash = (hash * 33) ^ key.weightedMode.GetHashCode();
                    hash = (hash * 33) ^ key.inWeight.GetHashCode();
                    hash = (hash * 33) ^ key.outWeight.GetHashCode();
                }
                
                return hash;
            }
        }

        public bool Equals(ObservableAnimationCurveParameter other)
            => value.keys.EqualsKeyframes(other.value.keys);
    }

    [System.Serializable]
    public class ObservableTexture2DParameter : Texture2DParameter, IObservableParameter<Texture>
    {
        private Texture m_Cached;

        public event NotifyDelegate<Texture> OnValueChanged;

        public ObservableTexture2DParameter(Texture value, bool overrideState = false)
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

    public class GradientKeyComparer : IEqualityComparer<GradientColorKey>, IEqualityComparer<GradientAlphaKey>
    {
        public bool Equals(GradientColorKey a, GradientColorKey b)
            => a.EqualsGradientColorKey(b);

        public int GetHashCode(GradientColorKey obj)
        {
            unchecked
            {
                int hash = obj.time.GetHashCode();
                hash = (hash * 397) ^ obj.color.GetHashCode();
                return hash;
            }
        }

        public bool Equals(GradientAlphaKey a, GradientAlphaKey b)
            => a.EqualsGradientAlphaKey(b);

        public int GetHashCode(GradientAlphaKey obj)
        {
            unchecked
            {
                int hash = obj.time.GetHashCode();
                hash = (hash * 397) ^ obj.alpha.GetHashCode();
                return hash;
            }
        }
    }

    public static class GradientKeyExtensions
    {
        public static bool EqualsGradientColorKey(this GradientColorKey a, GradientColorKey b, float epsilon = 0.0001f)
        {
            return
                a.color == b.color &&
                Mathf.Abs(a.time - b.time) < epsilon;
        }

        public static bool EqualsGradientColorKeys(this GradientColorKey[] a, GradientColorKey[] b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null || a.Length != b.Length)
                return false;

            return a.SequenceEqual(b, new GradientKeyComparer());
        }

        public static bool EqualsGradientAlphaKey(this GradientAlphaKey a, GradientAlphaKey b, float epsilon = 0.0001f)
        {
            return
                Mathf.Abs(a.alpha - b.alpha) < epsilon &&
                Mathf.Abs(a.time - b.time) < epsilon;
        }

        public static bool EqualsGradientAlphaKeys(this GradientAlphaKey[] a, GradientAlphaKey[] b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null || a.Length != b.Length)
                return false;

            return a.SequenceEqual(b, new GradientKeyComparer());
        }
    }

    public static class TextureGradientExtensions
    {
        public static void CopyFromTextureGradient(this Gradient gradient, TextureGradient other)
        {
            gradient.mode = other.mode;
            gradient.colorSpace = other.colorSpace;

            gradient.colorKeys = other.colorKeys.ToArray();
            gradient.alphaKeys = other.alphaKeys.ToArray();
            /*
            if (gradient.colorKeys.Length != other.colorKeys.Length)
            {
                gradient.colorKeys = new GradientColorKey[other.colorKeys.Length];
            }
            if (gradient.alphaKeys.Length != other.alphaKeys.Length)
            {
                gradient.alphaKeys = new GradientAlphaKey[other.alphaKeys.Length];
            }
            Array.Copy(other.colorKeys, gradient.colorKeys, gradient.colorKeys.Length);
            Array.Copy(other.alphaKeys, gradient.alphaKeys, gradient.alphaKeys.Length);
            */
        }

        public static void CopyFromTextureGradient(this TextureGradient gradient, TextureGradient other)
            => gradient.SetKeys(other.colorKeys, other.alphaKeys, other.mode, other.colorSpace);

        public static bool EqualsTextureGradient(this Gradient gradient, TextureGradient other)
        {
            return
                gradient.colorKeys.EqualsGradientColorKeys(other.colorKeys) &&
                gradient.alphaKeys.EqualsGradientAlphaKeys(other.alphaKeys) &&
                gradient.mode == other.mode &&
                gradient.colorSpace == other.colorSpace;
        }

        public static bool EqualsTextureGradient(this TextureGradient gradient, TextureGradient other)
        {
            return
                gradient.colorKeys.EqualsGradientColorKeys(other.colorKeys) &&
                gradient.alphaKeys.EqualsGradientAlphaKeys(other.alphaKeys) &&
                gradient.mode == other.mode &&
                gradient.colorSpace == other.colorSpace;
        }
    }

    [System.Serializable]
    public class ObservableTextureGradientParameter : TextureGradientParameter, IObservableParameter<TextureGradient>, IEquatable<ObservableTextureGradientParameter>
    {
        private int m_Cached;

        public event NotifyDelegate<TextureGradient> OnValueChanged;

        private int _cache;

        public ObservableTextureGradientParameter(TextureGradient value, bool overrideState = false)
            : base(value, overrideState)
        { }

        public override TextureGradient value
        {
            get => base.value;
            set
            {
                var hash = value.GetHashCode();
                if (_cache != hash)
                {
                    _cache = hash;
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
            var hash = GetHashCode();
            if(m_Cached != hash)
            {
                m_Cached = hash;
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            if (value == null || value.colorKeys == null || value.colorKeys.Length == 0 || value.alphaKeys == null || value.alphaKeys.Length == 0)
                return 0;

            unchecked
            {
                int hash = 5381;

                foreach (var key in value.colorKeys)
                {
                    hash = (hash * 33) ^ key.color.GetHashCode();
                    hash = (hash * 33) ^ key.time.GetHashCode();
                }

                foreach (var key in value.alphaKeys)
                {
                    hash = (hash * 33) ^ key.alpha.GetHashCode();
                    hash = (hash * 33) ^ key.time.GetHashCode();
                }

                hash = (hash * 33) ^ value.mode.GetHashCode();
                hash = (hash * 33) ^ value.colorSpace.GetHashCode();

                return hash;
            }
        }

        public bool Equals(ObservableTextureGradientParameter other)
            => value.EqualsTextureGradient(other.value);
    }
}