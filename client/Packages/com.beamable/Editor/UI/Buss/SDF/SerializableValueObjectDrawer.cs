using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beamable.UI.SDF.Styles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Beamable.Editor.UI.SDF {
    [CustomPropertyDrawer(typeof(SerializableValueImplementsAttribute))]
    [CustomPropertyDrawer(typeof(SerializableValueObject))]
    public class SerializableValueObjectDrawer : PropertyDrawer {
        private float _cachedHeight = 0f;
        private static readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();
        private static SerializableValueImplementsAttribute q = new SerializableValueImplementsAttribute(typeof(ISDFProperty));
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            
            var rc = new EditorGUIRectController(position);
            
            var typeProperty = property.FindPropertyRelative("type");
            var jsonProperty = property.FindPropertyRelative("json");
            var type = typeProperty.stringValue;
            var json = jsonProperty.stringValue;

            var hasChange = false;
            
            Type sysType = null;
            object value = null;
            
            var implementsAtt = (SerializableValueImplementsAttribute) attribute;
            if (implementsAtt != null) {
                var dropdownRect = position;
                dropdownRect.x += EditorGUIUtility.labelWidth;
                dropdownRect.width -= EditorGUIUtility.labelWidth;
                dropdownRect.height = EditorGUIUtility.singleLineHeight;
                var types = implementsAtt.subTypes;
                var dropdownIndex = Array.IndexOf(types, Type.GetType(type));
                var newIndex = EditorGUI.Popup(dropdownRect, dropdownIndex, implementsAtt.labels);
                if (dropdownIndex != newIndex && newIndex != -1) {
                    hasChange = true;
                    sysType = types[newIndex];
                    type = sysType?.AssemblyQualifiedName;
                    if (sysType == null) {
                        json = null;
                    }
                    else {
                        value = Activator.CreateInstance(sysType);
                        json = JsonUtility.ToJson(value);
                    }
                }
            }
            
            sysType = type == null ? null : Type.GetType(type);
            value = JsonUtility.FromJson(json, sysType);
            EditorGUI.BeginChangeCheck();
            value = EditorGUIExtension.DrawObject(rc, label, value, _foldouts, property.serializedObject.targetObject.GetInstanceID() + ":" + property.propertyPath);
            hasChange |= EditorGUI.EndChangeCheck();
            
            if (hasChange) {
                typeProperty.stringValue = sysType?.AssemblyQualifiedName;
                jsonProperty.stringValue = value != null ? JsonUtility.ToJson(value) : null;
                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();
            }
            
            _cachedHeight = position.height - rc.rect.height;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return _cachedHeight;
        }
    }
}