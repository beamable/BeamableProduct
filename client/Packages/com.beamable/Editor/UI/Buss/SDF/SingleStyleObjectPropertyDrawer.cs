using System.Linq;
using Beamable.UI.SDF;
using Beamable.UI.SDF.Styles;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.SDF {
    [CustomPropertyDrawer(typeof(SingleStyleObject))]
    public class SingleStyleObjectPropertyDrawer : PropertyDrawer {
        
        private SerializableValueObjectDrawer _svoDrawer = new SerializableValueObjectDrawer();
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var rc = position.ToRectController();
            
            EditorGUI.LabelField(rc.ReserveSingleLine(), label);
            rc.MoveIndent(1);
            EditorGUI.PropertyField(rc.ReserveSingleLine(), property.FindPropertyRelative("_name"));

            var properties = property.FindPropertyRelative("_properties");
            var keys = new string[properties.arraySize];
            rc.MoveIndent(2);
            for (int i = 0; i < properties.arraySize; i++) {
                rc.ReserveHeight(5f);
                var element = properties.GetArrayElementAtIndex(i);
                var key = element.FindPropertyRelative("key").stringValue;
                var prop = element.FindPropertyRelative("property");
                keys[i] = key;
                var elementLabel = new GUIContent(key);
                _svoDrawer.baseTypeOverride = SDFStyle.GetBaseType(key);
                var rect = rc.ReserveHeight(_svoDrawer.GetPropertyHeight(prop, elementLabel));
                GUI.Box(rect, GUIContent.none);
                _svoDrawer.OnGUI(rect, prop, elementLabel);
                _svoDrawer.baseTypeOverride = null;
            }
            
            rc.MoveIndent(-3);

            if (GUI.Button(rc.ReserveWidthFromRight(30f), "-")) {
                var context = new GenericMenu();
                foreach (var key in keys) {
                    context.AddItem(new GUIContent(key), false, () => {
                        var idx = 0;
                        var element = properties.GetArrayElementAtIndex(0);
                        while (element.FindPropertyRelative("key").stringValue != key) {
                            idx++;
                            element = properties.GetArrayElementAtIndex(idx);
                        }
                        properties.DeleteArrayElementAtIndex(idx);
                        properties.serializedObject.ApplyModifiedProperties();
                    });
                }
                context.ShowAsContext();
            }
            
            if (GUI.Button(rc.ReserveWidthFromRight(30f), "+")) {
                var context = new GenericMenu();
                foreach (var key in SDFStyle.Keys) {
                    if(keys.Contains(key)) continue;
                    context.AddItem(new GUIContent(key), false, () => {
                        var index = properties.arraySize;
                        properties.InsertArrayElementAtIndex(index);
                        var element = properties.GetArrayElementAtIndex(index);
                        element.FindPropertyRelative("key").stringValue = key;
                        var defaultProperty = SDFStyle.GetDefaultValue(key);
                        var prop = element.FindPropertyRelative("property");
                        prop.FindPropertyRelative("type").stringValue = defaultProperty.GetType().AssemblyQualifiedName;
                        prop.FindPropertyRelative("json").stringValue = JsonUtility.ToJson(defaultProperty);
                        properties.serializedObject.ApplyModifiedProperties();
                    });
                }
                context.ShowAsContext();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            var height = EditorGUIUtility.singleLineHeight * 3f;

            var properties = property.FindPropertyRelative("_properties");
            for (int i = 0; i < properties.arraySize; i++) {
                var element = properties.GetArrayElementAtIndex(i);
                var key = element.FindPropertyRelative("key").stringValue;
                var prop = element.FindPropertyRelative("property");
                var elementLabel = new GUIContent(key);
                _svoDrawer.baseTypeOverride = SDFStyle.GetBaseType(key);
                height += _svoDrawer.GetPropertyHeight(prop, elementLabel);
                _svoDrawer.baseTypeOverride = null;
            }

            height += properties.arraySize * 5f;

            return height;
        }
    }
}