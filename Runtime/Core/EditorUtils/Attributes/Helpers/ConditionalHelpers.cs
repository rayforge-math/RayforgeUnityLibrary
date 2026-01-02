using System;
using System.Collections;
using UnityEngine;

namespace Rayforge.EditorUtils.Attributes.Helpers
{
    public static class ConditionalHelpers
    {
        public static bool Compare(object depValue, object compareValue)
        {
            if (depValue == null || compareValue == null)
                return false;

            if (compareValue is IEnumerable enumerable && !(compareValue is string))
            {
                foreach (var item in enumerable)
                {
                    if (Compare(depValue, item))
                        return true;
                }
                return false;
            }

            if (depValue is float f && compareValue is float cf)
                return Mathf.Approximately(f, cf);

            var depType = depValue.GetType();
            var compareType = compareValue.GetType();

            if (depType.IsEnum && compareType.IsEnum)
                return Convert.ToInt64(depValue) == Convert.ToInt64(compareValue);

            if (depType.IsEnum && compareValue is int ci)
                return Convert.ToInt64(depValue) == ci;

            if (depValue is int di && compareType.IsEnum)
                return di == Convert.ToInt64(compareValue);

            return depValue.Equals(compareValue);
        }
    }
}