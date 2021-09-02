using System.Collections.Generic;
using Beamable.Common.Content;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Content.UI
{
#if !BEAMABLE_NO_DICT_DRAWERS
   [CustomPropertyDrawer(typeof(SerializableDictionaryStringToString), true)]
#endif
   public class SerializableDictionaryStringToStringEditor : SerializedDictionaryStringToSomethingEditor<string>
   {
      protected override string RenderNextValue(Rect rect, KeyValuePair<string, string> kvp)
      {
         return EditorGUI.TextField(rect, kvp.Key, kvp.Value);
      }
   }

#if !BEAMABLE_NO_DICT_DRAWERS
   [CustomPropertyDrawer(typeof(SerializableDictionaryStringToInt), true)]
#endif
   public class SerializableDictionaryStringToIntEditor : SerializedDictionaryStringToSomethingEditor<int>
   {
      protected override int RenderNextValue(Rect rect, KeyValuePair<string, int> kvp)
      {
         return EditorGUI.IntField(rect, kvp.Key, kvp.Value);
      }
   }


   public abstract class SerializedDictionaryStringToSomethingEditor<TValue> : PropertyDrawer
   {
      private string _addKey;

      protected abstract TValue RenderNextValue(Rect rect, KeyValuePair<string, TValue> kvp);

      protected virtual TValue ProduceDefaultValue(string key) => default;

      public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
      {
         var target =
            ContentRefPropertyDrawer.GetTargetObjectOfProperty(property) as SerializableDictionaryStringToSomething<TValue>;

         if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

         return EditorGUIUtility.singleLineHeight * (3 + target.Count); // the title, labels, buttons, and rows.
      }

      public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
      {
         var target =
            ContentRefPropertyDrawer.GetTargetObjectOfProperty(property) as SerializableDictionaryStringToSomething<TValue>;

         var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
         var nextFoldout = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);
         property.isExpanded = nextFoldout;

         if (!nextFoldout) return;

         var labelRect = new Rect(position.x + 10, foldoutRect.yMax, position.width - 10, foldoutRect.height);
         EditorGUI.LabelField(labelRect, "(keys)", "(values)");

         var kvpY = labelRect.yMax;
         foreach (var kvp in target)
         {
            EditorGUI.BeginChangeCheck();


            var kvpRect = new Rect(labelRect.x, kvpY, labelRect.width - 20, EditorGUIUtility.singleLineHeight);
            kvpY += EditorGUIUtility.singleLineHeight;
            var nextValue = RenderNextValue(kvpRect, kvp);

            var xButtonRect = new Rect(kvpRect.xMax, kvpRect.yMin, 18, kvpRect.height);
            var shouldDeleteKey = GUI.Button(xButtonRect, "X");


            if (shouldDeleteKey)
            {
               target.Remove(kvp.Key);
               MarkDirty(property);
               break;
            }
            if (EditorGUI.EndChangeCheck())
            {
               target[kvp.Key] = nextValue;
               MarkDirty(property);
               break;
            }

         }


         var newKeyRect = new Rect(labelRect.x, kvpY, labelRect.width - 84, EditorGUIUtility.singleLineHeight);
         _addKey = EditorGUI.TextField(newKeyRect, " ", _addKey);
         var isValidKey = _addKey != null && _addKey.Length > 0 && !target.ContainsKey(_addKey);
         var wasEnabled = GUI.enabled;
         GUI.enabled = isValidKey;

         var addKeyButtonRect = new Rect(newKeyRect.xMax + 2, kvpY, 80, newKeyRect.height);
         if (GUI.Button(addKeyButtonRect, "Add Key"))
         {
            target.Add(_addKey, ProduceDefaultValue(_addKey));
            MarkDirty(property);
         }
         GUI.enabled = wasEnabled;


      }

      void MarkDirty(SerializedProperty prop)
      {
         EditorUtility.SetDirty(prop.serializedObject.targetObject);
         if (prop.serializedObject.targetObject is ContentObject contentObject)
         {
            contentObject.ForceValidate();
         }
      }
   }
}