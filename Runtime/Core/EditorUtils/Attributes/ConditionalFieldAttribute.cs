using UnityEngine;
using Rayforge.EditorUtils.Attributes.Helpers;
using System.Collections.Generic;
using Rayforge.EditorUtils.Attributes.Abstractions;
using System;

namespace Rayforge.EditorUtils.Attributes
{
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
}