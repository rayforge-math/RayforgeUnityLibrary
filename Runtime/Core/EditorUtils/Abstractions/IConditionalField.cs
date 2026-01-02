using System;
using System.Collections.Generic;

namespace Rayforge.EditorUtils.Attributes.Abstractions
{
    public interface IConditionalField
    {
        public IEnumerable<(string field, object value, bool invert)> DependentFields { get; }
        public DrawMode DrawMode { get; }
        public bool Invert { get; }
        public bool CheckConditions(Func<string, object> getValue);
    }
}