using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

using Rayforge.Core.Utility.VolumeComponents.Parameters;

namespace Rayforge.Core.Utility.VolumeComponents.Editor
{
    [VolumeParameterDrawer(typeof(ObservableClampedFloatParameter))]
    public class ObservableClampedFloatParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Float)
                return false;

            var o = parameter.GetObjectRef<ObservableClampedFloatParameter>();
            EditorGUILayout.Slider(value, o.min, o.max, title);
            value.floatValue = Mathf.Clamp(value.floatValue, o.min, o.max);
            return true;
        }
    }
}