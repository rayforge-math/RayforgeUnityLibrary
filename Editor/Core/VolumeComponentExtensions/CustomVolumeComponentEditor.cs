using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.VolumeComponentExtensions.Editor
{
    public static class SerializedDataParameterExtensions
    {
        public static bool ReferenceEquals(this SerializedDataParameter a, SerializedDataParameter b)
        {
            return a.GetObjectRef<object>() == b.GetObjectRef<object>();
        }
    }

    public abstract class CustomVolumeComponentEditor<Tcomp> : VolumeComponentEditor
        where Tcomp : VolumeComponent
    {
        protected struct ParameterEntry
        {
            public GUIContent displayName;
            public int displayOrder;
            public SerializedDataParameter param;
        }

        private List<ParameterEntry> m_Parameters;

        private Dictionary<string, bool> m_FoldoutStates = new Dictionary<string, bool>();

        private PropertyFetcher<Tcomp> m_Fetcher;
        private string m_CurrentFoldout;

        public override void OnEnable()
        {
            var fields = new List<(FieldInfo, SerializedProperty)>();
            GetFields(target, fields);

            m_Parameters = fields
                .Select(t =>
                {
                    var name = "";
                    var order = 0;
                    var (fieldInfo, serializedProperty) = t;
                    var attr = (DisplayInfoAttribute[])fieldInfo.GetCustomAttributes(typeof(DisplayInfoAttribute), true);
                    if (attr.Length != 0)
                    {
                        name = attr[0].name;
                        order = attr[0].order;
                    }

                    var parameter = Unpack(t.Item2);
                    return new ParameterEntry { displayName = EditorGUIUtility.TrTextContent(name), displayOrder = order, param = parameter };
                })
                .OrderBy(t => t.displayOrder)
                .ToList();

            m_Fetcher = new PropertyFetcher<Tcomp>(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            for (int i = 0; i < m_Parameters.Count; ++i)
            {
                var parameter = m_Parameters[i];
                bool last = m_Parameters.Count - 1 == i;

                var drawMode = CheckFoldout(parameter);

                if (drawMode == DrawMode.Draw)
                {
                    drawMode = CheckConditionals(parameter);

                    switch (drawMode)
                    {
                        case DrawMode.Draw:
                            CheckSeparator(parameter, false);
                            DrawVolumeParameter(parameter.param, parameter.displayName);
                            CheckSeparator(parameter, true);
                            break;
                        case DrawMode.Disabled:
                            CheckSeparator(parameter, false);
                            EditorGUI.BeginDisabledGroup(true);
                            DrawVolumeParameter(parameter.param, parameter.displayName);
                            EditorGUI.EndDisabledGroup();
                            CheckSeparator(parameter, true);
                            break;
                        default:
                        case DrawMode.Hidden:
                            break;
                    }
                }

                CheckLastFoldout(last);
            }
        }

        private void CheckSeparator(ParameterEntry parameter, bool below)
        {
            var separatorAttr = parameter.param.GetAttribute<LineSeparatorAttribute>();
            if (separatorAttr != null)
            {
                bool draw = below == separatorAttr.below;
                if (draw)
                {
                    DrawSeparator(separatorAttr);
                }
            }
        }

        private DrawMode CheckConditionals(ParameterEntry parameter)
        {
            var drawMode = CheckConditional<ConditionalFieldAttribute>(parameter);
            if (drawMode == DrawMode.Draw)
            {
                drawMode = CheckConditional<ConditionalFieldsAttribute>(parameter);
            }
            return drawMode;
        }

        private DrawMode CheckConditional<Tattr>(ParameterEntry parameter)
            where Tattr : PropertyAttribute, IConditionalField
        {
            var drawMode = DrawMode.Draw;

            var conditionalAttr = parameter.param.GetAttribute<Tattr>();
            if (conditionalAttr != null)
            {
                bool show = conditionalAttr.CheckConditions(fieldName =>
                {
                    var depProp = m_Fetcher.Find(fieldName);
                    if (depProp == null)
                        return null;

                    return Unpack(depProp).value.boxedValue;
                });

                drawMode = show ? DrawMode.Draw : conditionalAttr.DrawMode;
            }

            return drawMode;
        }

        private DrawMode CheckFoldout(ParameterEntry parameter)
        {
            var foldoutAttr = parameter.param.GetAttribute<FoldoutAttribute>();
            string groupName = foldoutAttr != null ? foldoutAttr.groupName : null;

            if (m_CurrentFoldout != null && m_CurrentFoldout != groupName)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                m_CurrentFoldout = null;
            }

            if (groupName != null)
            {
                if (m_CurrentFoldout != groupName)
                {
                    bool open = m_FoldoutStates.ContainsKey(groupName) ? m_FoldoutStates[groupName] : false;
                    open = EditorGUILayout.BeginFoldoutHeaderGroup(open, groupName);
                    m_FoldoutStates[groupName] = open;
                    m_CurrentFoldout = groupName;
                }

                if (!m_FoldoutStates[groupName]) return DrawMode.Hidden;
            }

            return DrawMode.Draw;
        }

        private void CheckLastFoldout(bool last)
        {
            if (last && m_CurrentFoldout != null)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private void DrawVolumeParameter(SerializedDataParameter param, GUIContent displayName)
        {
            if (!string.IsNullOrEmpty(displayName.text))
                PropertyField(param, displayName);
            else
                PropertyField(param);
        }

        private void GetFields(object o, List<(FieldInfo, SerializedProperty)> infos, SerializedProperty prop = null)
        {
            if (o == null)
                return;

            var fields = o.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType.IsSubclassOf(typeof(VolumeParameter)))
                {
                    if ((field.GetCustomAttributes(typeof(HideInInspector), false).Length == 0) &&
                        ((field.GetCustomAttributes(typeof(SerializeField), false).Length > 0) ||
                         (field.IsPublic && field.GetCustomAttributes(typeof(NonSerializedAttribute), false).Length == 0)))
                        infos.Add((field, prop == null ? serializedObject.FindProperty(field.Name) : prop.FindPropertyRelative(field.Name)));
                }
                else if (!field.FieldType.IsArray && field.FieldType.IsClass)
                    GetFields(field.GetValue(o), infos, prop == null ? serializedObject.FindProperty(field.Name) : prop.FindPropertyRelative(field.Name));
            }
        }

        private void DrawSeparator(LineSeparatorAttribute attr)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, attr.thickness);
            rect.height = attr.thickness;


            EditorGUI.DrawRect(rect, attr.color);
        }

        private bool GetFoldoutState(string groupName)
        {
            if (!m_FoldoutStates.ContainsKey(groupName))
                m_FoldoutStates[groupName] = true;

            return m_FoldoutStates[groupName];
        }

        private void SetFoldoutState(string groupName, bool state)
        {
            m_FoldoutStates[groupName] = state;
        }
    }

}