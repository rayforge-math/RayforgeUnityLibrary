using UnityEngine;

namespace Rayforge.EditorUtils.Attributes
{
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