using Rayforge.EditorUtils.Attributes.Abstractions;
using Rayforge.EditorUtils.Attributes.Helpers;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Rayforge.EditorUtils.Attributes
{
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

            if (inverts == null)
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
}