using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.VolumeComponentExtensions.Editor
{
    public enum DrawMode
    {
        Draw = 0,
        Hidden = 1,
        Disabled = 2
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class FoldoutAttribute : PropertyAttribute
    {
        public string groupName;
        public FoldoutAttribute(string groupName) => this.groupName = groupName;
    }

    public interface IConditionalField
    {
        public IEnumerable<(string field, object value, bool invert)> DependentFields { get; }
        public DrawMode DrawMode { get; }
        public bool Invert { get; }
        public bool CheckConditions(Func<string, object> getValue);
    }

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

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class ConditionalFieldAttribute : PropertyAttribute, IConditionalField
    {
        public readonly string dependentField;
        public readonly object compareValue;
        public readonly DrawMode drawMode;
        public readonly bool invert;

        public ConditionalFieldAttribute(string dependentField, object compareValue, bool invert = false, DrawMode drawMode = DrawMode.Disabled)
        {
            this.dependentField = dependentField;
            this.compareValue = compareValue;
            this.drawMode = drawMode;
            this.invert = invert;
        }

        public IEnumerable<(string field, object value, bool invert)> DependentFields 
        {
            get { yield return (dependentField, compareValue, invert); } 
        }
        public DrawMode DrawMode => drawMode;
        public bool Invert => invert;

        public bool CheckConditions(Func<string, object> getValue)
        {
            object depValue = getValue(this.dependentField);
            object compareValue = this.compareValue;

            bool result = ConditionalHelpers.Compare(depValue, compareValue);

            if (invert)
                result = !result;

            return result;
        }
    }

    public enum ConditionalOperator { And, Or }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class ConditionalFieldsAttribute : PropertyAttribute, IConditionalField
    {
        public readonly string[] dependentFields;
        public readonly object[] compareValues;
        public readonly ConditionalOperator op;
        public readonly DrawMode drawMode;
        public readonly bool[] inverts;
        public readonly bool invert;

        public ConditionalFieldsAttribute(string[] dependentFields, object[] compareValues, bool[] inverts = null, bool invert = false, ConditionalOperator op = ConditionalOperator.And, DrawMode drawMode = DrawMode.Disabled)
        {
            Debug.Assert(dependentFields.Length == compareValues.Length, $"ConditionalFieldsAttribute: compareValues ({compareValues.Length})");
            if (inverts != null)
                Debug.Assert(dependentFields.Length == inverts.Length, $"ConditionalFieldsAttribute: invert ({inverts.Length})");

            this.dependentFields = dependentFields;
            this.compareValues = compareValues;
            this.op = op;
            this.drawMode = drawMode;
            this.invert = invert;

            if(inverts == null)
                this.inverts = new bool[dependentFields.Length];
            else
                this.inverts = inverts;
        }

        public IEnumerable<(string field, object value, bool invert)> DependentFields
        {
            get
            {
                for (int i = 0; i < dependentFields.Length; i++)
                    yield return (dependentFields[i], compareValues[i], inverts[i]);
            }
        }
        public DrawMode DrawMode => drawMode;
        public bool Invert => invert;

        public bool CheckConditions(Func<string, object> getValue)
        {
            bool result = (op == ConditionalOperator.And);

            for (int i = 0; i < dependentFields.Length; i++)
            {
                object depValue = getValue(dependentFields[i]);
                object compareValue = compareValues[i];

                bool conditionMet = ConditionalHelpers.Compare(depValue, compareValue);
                bool invert = inverts[i];

                if (op == ConditionalOperator.And)
                    result &= invert ? !conditionMet : conditionMet;
                else
                    result |= invert ? !conditionMet : conditionMet;
            }

            if (invert)
                result = !result;

            return result;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class LineSeparatorAttribute : PropertyAttribute
    {
        public readonly Color color;
        public readonly float thickness;
        public readonly bool below;

        public LineSeparatorAttribute(float thickness = 1f, bool below = false, float r = 0.3f, float g = 0.3f, float b = 0.3f)
        {
            this.color = new Color(r, g, b);
            this.thickness = thickness;
            this.below = below;
        }
    }
}