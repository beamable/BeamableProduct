using System.Linq;
using Beamable.UI.BUSS;
using Beamable.UI.SDF.Styles;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.SDF
{
    [CustomPropertyDrawer(typeof(BUSSStyleDescriptionWithSelector))]
    [CustomPropertyDrawer(typeof(BUSSStyleDescription))]
    public class BUSSStyleDescriptionDrawer : PropertyDrawer
    {
        private SerializableValueObjectDrawer _svoDrawer = new SerializableValueObjectDrawer();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rc = position.ToRectController();

            EditorGUI.LabelField(rc.ReserveSingleLine(), label);
            rc.MoveIndent(1);

            var nameProperty = property.FindPropertyRelative("_name");
            if (nameProperty != null) {
                EditorGUI.PropertyField(rc.ReserveSingleLine(), nameProperty);
            }

            var properties = property.FindPropertyRelative("_properties");
            var keys = new string[properties.arraySize];
            rc.MoveIndent(2);
            for (int i = 0; i < properties.arraySize; i++) {
                rc.ReserveHeight(5f);

                keys[i] = DrawSingleProperty(rc, i, properties);
            }

            rc.MoveIndent(-3);

            if (GUI.Button(rc.ReserveWidthFromRight(30f), "-")) {
                ShowDeleteContext(keys, properties);
            }

            if (GUI.Button(rc.ReserveWidthFromRight(30f), "+")) {
                ShowAddContext(keys, properties);
            }
        }

        private static void ShowDeleteContext(string[] keys, SerializedProperty properties) {
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

        private static void ShowAddContext(string[] keys, SerializedProperty properties) {
            var context = new GenericMenu();
            foreach (var key in BUSSStyle.Keys) {
                if (keys.Contains(key)) continue;
                context.AddItem(new GUIContent(key), false, () => {
                    var index = properties.arraySize;
                    properties.InsertArrayElementAtIndex(index);
                    var element = properties.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("key").stringValue = key;
                    var defaultProperty = BUSSStyle.GetDefaultValue(key);
                    var prop = element.FindPropertyRelative("property");
                    prop.FindPropertyRelative("type").stringValue = defaultProperty.GetType().AssemblyQualifiedName;
                    prop.FindPropertyRelative("json").stringValue = JsonUtility.ToJson(defaultProperty);
                    properties.serializedObject.ApplyModifiedProperties();
                });
            }
            
            context.AddItem(new GUIContent("Variable"), false, () => {
                VariableNameWizard.ShowWizard(key => {
                    if (!keys.Contains(key)) {
                        var index = properties.arraySize;
                        properties.InsertArrayElementAtIndex(index);
                        var element = properties.GetArrayElementAtIndex(index);
                        element.FindPropertyRelative("key").stringValue = key;
                        var prop = element.FindPropertyRelative("property");
                        prop.FindPropertyRelative("type").stringValue = "";
                        prop.FindPropertyRelative("json").stringValue = "";
                        properties.serializedObject.ApplyModifiedProperties();
                    }
                });
            });

            context.ShowAsContext();
        }

        private string DrawSingleProperty(EditorGUIRectController rc, int index, SerializedProperty properties) {
            var buttonRc = rc.ReserveWidth(0).ToRectController().ReserveSingleLine().ToRectController();
            buttonRc.ReserveWidthFromRight(15f);
            buttonRc.MoveIndent(-2);
            var buttonRect = buttonRc.ReserveHeightByFraction(.5f);
            if (index > 0 && GUI.Button(buttonRect, GUIContent.none)) {
                properties.MoveArrayElement(index, index - 1);
                properties.serializedObject.ApplyModifiedProperties();
            }

            if (index < properties.arraySize - 1 && GUI.Button(buttonRc.rect, GUIContent.none)) {
                properties.MoveArrayElement(index, index + 1);
                properties.serializedObject.ApplyModifiedProperties();
            }

            var element = properties.GetArrayElementAtIndex(index);
            var key = element.FindPropertyRelative("key").stringValue;
            var prop = element.FindPropertyRelative("property");
            var elementLabel = new GUIContent(key);
            _svoDrawer.baseTypeOverride = BUSSStyle.GetBaseType(key);
            var rect = rc.ReserveHeight(_svoDrawer.GetPropertyHeight(prop, elementLabel));
            GUI.Box(rect, GUIContent.none);
            _svoDrawer.OnGUI(rect, prop, elementLabel);
            _svoDrawer.baseTypeOverride = null;
            return key;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            var hasSelector = property.FindPropertyRelative("_name") != null;
            var height = EditorGUIUtility.singleLineHeight * (hasSelector ? 3f : 2f);

            var properties = property.FindPropertyRelative("_properties");
            for (int i = 0; i < properties.arraySize; i++)
            {
                var element = properties.GetArrayElementAtIndex(i);
                var key = element.FindPropertyRelative("key").stringValue;
                var prop = element.FindPropertyRelative("property");
                var elementLabel = new GUIContent(key);
                _svoDrawer.baseTypeOverride = BUSSStyle.GetBaseType(key);
                height += _svoDrawer.GetPropertyHeight(prop, elementLabel);
                _svoDrawer.baseTypeOverride = null;
            }

            height += properties.arraySize * 5f;

            return height;
        }
    }
}