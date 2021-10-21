using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor {
    public static class EditorGUIExtension {
        public static int IntFieldWithDropdown(Rect rect, int value, int[] dropdownValues, Action<int> onValueChange, string fieldFormat = null, GUIContent[] customDropdownLabels = null) {
            const float dropdownButtonWidth = 15f;
            const string darkModeDropdownIcon = "d_icon dropdown";
            const string lightModeDropdownIcon = "icon dropdown";
            var rc = new EditorGUIRectController(rect);
            value = GUIIntField(rc.ReserveWidth(rect.width - dropdownButtonWidth), value, fieldFormat);
            if (GUI.Button(rc.rect,
                EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? darkModeDropdownIcon : lightModeDropdownIcon))) {
                var hasCustomLabels =
                    customDropdownLabels != null && customDropdownLabels.Length >= dropdownValues.Length;
                var generic = new GenericMenu();
                for (var i = 0; i < dropdownValues.Length; i++) {
                    var dropdownValue = dropdownValues[i];
                    var label = hasCustomLabels ? customDropdownLabels[i] : new GUIContent(dropdownValue.ToString(fieldFormat));
                    generic.AddItem(label, false,
                        () => onValueChange(dropdownValue));
                }

                generic.DropDown(rect);
            }

            return value;
        }

        public static int GUIIntField(Rect rect, int value, string format = null) {
            var text = value.ToString(format);
            text = EditorGUI.DelayedTextField(rect, text);
            if (int.TryParse(text, out int newValue)) {
                return newValue;
            }

            return value;
        }

        public static FieldInfo GetFieldInfo(this SerializedProperty property) {
            var parentType = property.serializedObject.targetObject.GetType();
            FieldInfo fieldInfo = null;
            var pathParts = property.propertyPath.Split('.');
            for (int i = 0; i < pathParts.Length; i++) {
                fieldInfo = parentType.GetField(pathParts[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                parentType = fieldInfo.FieldType;
            }

            return fieldInfo;
        }

        public static Type GetParentType(this SerializedProperty property) {
            var parentType = property.serializedObject.targetObject.GetType();
            var pathParts = property.propertyPath.Split('.');
            for (int i = 0; i < pathParts.Length - 1; i++) {
                var fieldInfo = parentType.GetField(pathParts[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                parentType = fieldInfo.FieldType;
            }

            return parentType;
        }

        public static object GetParentObject(this SerializedProperty property) {
            object parent = property.serializedObject.targetObject;
            var pathParts = property.propertyPath.Split('.');
            for (int i = 0; i < pathParts.Length - 1; i++) {
                var fieldInfo = parent.GetType().GetField(pathParts[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                parent = fieldInfo.GetValue(parent);
            }

            return parent;
        }

        public static void DrawField(EditorGUIRectController rc, object target, FieldInfo fieldInfo,  Dictionary<string, bool> foldouts, string path) {
            if (fieldInfo.GetCustomAttribute<NonSerializedAttribute>() != null) return;
            if (!fieldInfo.IsPublic && fieldInfo.GetCustomAttribute<SerializeField>() == null) return;
            var fieldLabel = fieldInfo.Name;
            var value = fieldInfo.GetValue(target);
            var delayed = fieldInfo.GetCustomAttribute<DelayedAttribute>() != null;

            value = DrawObject(rc, new GUIContent(fieldLabel), value, foldouts, $"{path}.{fieldLabel}", delayed);
            fieldInfo.SetValue(target, value);
        }

        public static object DrawObject(EditorGUIRectController rc, GUIContent label, object value, Dictionary<string, bool> foldouts, string path = "", bool delayed = false) {
            switch (value) {
                case null:
                    EditorGUI.LabelField(rc.ReserveSingleLine(), label);
                    return null;
                // --- Simple types
                case int i when delayed:
                    return EditorGUI.DelayedIntField(rc.ReserveSingleLine(), label, i);
                case int i:
                    return EditorGUI.IntField(rc.ReserveSingleLine(), label, i);
                case float f when delayed:
                    return EditorGUI.DelayedFloatField(rc.ReserveSingleLine(), label, f);
                case float f:
                    return EditorGUI.FloatField(rc.ReserveSingleLine(), label, f);
                case bool b:
                    return EditorGUI.Toggle(rc.ReserveSingleLine(), label, b);
                case string s when delayed:
                    return EditorGUI.DelayedTextField(rc.ReserveSingleLine(), label, s);
                case string s:
                    return EditorGUI.TextField(rc.ReserveSingleLine(), label, s);
                case Color color:
                    return EditorGUI.ColorField(rc.ReserveSingleLine(), label, color);
                case Vector2 v2:
                    return EditorGUI.Vector2Field(rc.ReserveSingleLine(), label, v2);
                case Vector3 v3:
                    return EditorGUI.Vector3Field(rc.ReserveSingleLine(), label, v3);
                case Vector4 v4:
                    return EditorGUI.Vector4Field(rc.ReserveSingleLine(), label, v4);
                case Vector2Int v2:
                    return EditorGUI.Vector2IntField(rc.ReserveSingleLine(), label, v2);
                case Vector3Int v3:
                    return EditorGUI.Vector3IntField(rc.ReserveSingleLine(), label, v3);

                // --- Arrays or lists
                case Array array:
                    if (DrawFoldout(rc, label, foldouts, path)) {
                        rc.MoveIndent(1);
                        for(int index = 0; index < array.Length; index++) {
                            var elementPath = $"{path}_{index}";
                            array.SetValue(DrawObject(rc, new GUIContent($"Element {index}"), array.GetValue(index),
                                foldouts, elementPath, delayed), index);
                        }
                        rc.MoveIndent(-1);
                    }
                    return array;
                
                case IList list:
                    if (DrawFoldout(rc, label, foldouts, path)) {
                        rc.MoveIndent(1);
                        for(int index = 0; index < list.Count; index++){
                            var elementPath = $"{path}_{index}";
                            list[index] = DrawObject(rc, new GUIContent($"Element {index++}"), list[index],
                                foldouts, elementPath, delayed);
                        }
                        rc.MoveIndent(-1);
                    }
                    return list;
                
                // --- Other objects
                default:
                    if (DrawFoldout(rc, label, foldouts, path)) {
                        rc.MoveIndent(1);
                        foreach (var fieldInfo in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                            DrawField(rc, value, fieldInfo, foldouts, path);
                        }
                        rc.MoveIndent(-1);
                    }
                    return value;
            }
        }

        private static bool DrawFoldout(EditorGUIRectController rc, GUIContent label, Dictionary<string, bool> foldouts, string path) {
            var expanded = false;
            if (foldouts.ContainsKey(path)) {
                expanded = foldouts[path];
            }

            expanded = EditorGUI.Foldout(rc.ReserveSingleLine(), expanded, label);
            foldouts[path] = expanded;
            return expanded;
        }
        
        public static EditorGUIRectController ToRectController(this Rect rect) => new EditorGUIRectController(rect);
    }
}