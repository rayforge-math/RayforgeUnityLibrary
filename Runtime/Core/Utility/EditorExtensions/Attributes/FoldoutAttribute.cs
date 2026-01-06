using UnityEngine;

namespace Rayforge.Utility.EditorExtensions.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class FoldoutAttribute : PropertyAttribute
    {
        public string groupName;
        public FoldoutAttribute(string groupName) => this.groupName = groupName;
    }
}