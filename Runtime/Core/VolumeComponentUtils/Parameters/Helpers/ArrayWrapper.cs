using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayforge.VolumeComponentUtils.Parameters.Helpers
{
    [System.Serializable]
    public struct ArrayWrapper<T> : IEnumerable<T>, IEquatable<ArrayWrapper<T>>
        where T : struct
    {
        [SerializeField]
        public T[] array;

        public int Length => array.Length;
        public T this[int index] => array[index];

        public ArrayWrapper(T[] array)
        {
            if (array != null && array.Length > 0)
            {
                this.array = new T[array.Length];
                Array.Copy(array, this.array, array.Length);
            }
            else
            {
                this.array = Array.Empty<T>();
            }
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

        public bool ReferenceEquals(ArrayWrapper<T> other)
            => ReferenceEquals(this.array, other.array);

        public bool Equals(ArrayWrapper<T> other)
        {
            if (ReferenceEquals(array, other.array)) return true;
            if (IsValid() != other.IsValid()) return false;
            if (!IsValid() && !other.IsValid()) return true;
            if (Length != other.Length) return false;

            return array.SequenceEqual(other.array);
        }

        public static ArrayWrapper<T> Empty()
            => new ArrayWrapper<T>(0);

        public void CopyFrom(ArrayWrapper<T> other)
        {
            if (other.IsValid())
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
}