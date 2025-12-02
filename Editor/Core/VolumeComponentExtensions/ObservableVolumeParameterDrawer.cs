using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace Rayforge.VolumeComponentExtensions.Editor
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

    [VolumeParameterDrawer(typeof(ObservableClampedIntParameter))]
    public class ObservableClampedIntParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Integer)
                return false;

            var o = parameter.GetObjectRef<ObservableClampedIntParameter>();
            EditorGUILayout.IntSlider(value, o.min, o.max, title);
            value.intValue = (int)Mathf.Clamp(value.intValue, o.min, o.max);
            return true;
        }
    }
}