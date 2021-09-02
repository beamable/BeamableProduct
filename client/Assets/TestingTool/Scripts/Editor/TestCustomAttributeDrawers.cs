using TestingTool.Scripts.Attributes;
using UnityEditor;
using UnityEngine;

namespace TestingTool.Scripts.Editor
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true;
        }
    }
    [CustomPropertyDrawer(typeof(RequiredFieldAttribute))]
    public class RequiredFieldDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(property.objectReferenceValue == null)
            {
                GUI.color = Color.red;
                EditorGUI.PropertyField(position, property, label);
                GUI.color = Color.white;
            }
            else
                EditorGUI.PropertyField(position, property, label);
        }
    }
    [CustomPropertyDrawer(typeof(StatusVerifierAttribute))]
    public class StatusVerifierDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            switch ((ProgressStatus)property.enumValueIndex)
            {
                case ProgressStatus.NotSet: GUI.color = Color.gray; break;
                case ProgressStatus.NotPassed: GUI.color = Color.red; break;
                case ProgressStatus.Pending: GUI.color = Color.yellow; break;
                case ProgressStatus.Passed: GUI.color = Color.green; break;
            }
            
            EditorGUI.PropertyField(position, property, label);
            GUI.color = Color.white;
        }
    }
}