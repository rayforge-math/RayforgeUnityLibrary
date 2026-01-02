using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace Rayforge.VolumeComponentUtils.Parameters
{
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