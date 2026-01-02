using UnityEngine;

namespace Rayforge.EditorUtils.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class FoldoutAttribute : PropertyAttribute
    {
        public string groupName;
        public FoldoutAttribute(string groupName) => this.groupName = groupName;
    }
}