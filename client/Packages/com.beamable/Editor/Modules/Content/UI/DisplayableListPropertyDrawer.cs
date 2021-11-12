using System;
using Beamable.Common.Content;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Content.UI
{

   #if !BEAMABLE_NO_LIST_DRAWERS
   [CustomPropertyDrawer(typeof(DisplayableList), true)]
   #endif
   public class DisplayableListPropertyDrawer: PropertyDrawer
   {
      public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
      {
         var list = ContentRefPropertyDrawer.GetTargetObjectOfProperty(property) as DisplayableList;
         var subProp = property.FindPropertyRelative(list.GetListPropertyPath());
         if (subProp == null)
         {
            Debug.Log("Its a null ref in the displayable list!");
            Debug.Log("- " + property.propertyPath);
            while (property.Next(true))
            {
               Debug.Log("--- " + property.propertyPath);
            }

            return 0;
         }
         return EditorGUI.GetPropertyHeight(subProp);
      }

      public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
      {
         var list = ContentRefPropertyDrawer.GetTargetObjectOfProperty(property) as DisplayableList;

         label.tooltip = PropertyDrawerHelper.SetTooltipWithFallback(fieldInfo, property);

         var subProp = property.FindPropertyRelative(list.GetListPropertyPath());
         if (EditorGUI.PropertyField(position, subProp, label, true))
         {
            MarkDirty(property);
         }

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