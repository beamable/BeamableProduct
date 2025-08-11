using Beamable.Common.Content;
using Beamable.Content.Utility;
using Beamable.Editor.Util;
using System;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Content
{
	[CustomPropertyDrawer(typeof(EventSchedule))]
	public class EventSchedulePropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			
			Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

			if (property.isExpanded)
			{
				EditorGUI.indentLevel++;
				float yOffset = BeamGUI.StandardVerticalSpacing;
				
				SerializedProperty descriptionProp = property.FindPropertyRelative("description");
				Rect descriptionRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
				EditorGUI.PropertyField(descriptionRect, descriptionProp);
				yOffset += BeamGUI.StandardVerticalSpacing;
				
				
				SerializedProperty activeToProp = property.FindPropertyRelative("activeTo");
				float activeToSize = position.width < DatePropertyDrawer.SingleLineWidth
					? EditorGUIUtility.singleLineHeight * 2f
					: EditorGUIUtility.singleLineHeight;
				Rect activeToRect = new Rect(position.x, position.y + yOffset, position.width, activeToSize);
				EditorGUI.PropertyField(activeToRect, activeToProp);
				yOffset += activeToSize;
				var hasValueProp = activeToProp.FindPropertyRelative(nameof(Optional.HasValue));
				if (hasValueProp.boolValue)
				{
					var startDate = property.serializedObject.FindProperty("startDate");
					var startDateTime = DateTime.ParseExact(startDate.stringValue, DateUtility.ISO_FORMAT, null);
					var activeToStringProp = activeToProp.FindPropertyRelative("Value");
					var endDateTime = DateTime.ParseExact(activeToStringProp.stringValue, DateUtility.ISO_FORMAT, null);
					if (endDateTime < startDateTime)
					{
						activeToStringProp.stringValue = startDateTime.ToString(DateUtility.ISO_FORMAT);
					}
				}


				SerializedProperty definitionsProp = property.FindPropertyRelative("definitions");
				Rect definitionsRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
				EditorGUI.PropertyField(definitionsRect, definitionsProp, true);
            
				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!property.isExpanded)
			{
				return EditorGUIUtility.singleLineHeight;
			}

			float height = BeamGUI.StandardVerticalSpacing * 2; // Label + Description

			if (EditorGUIUtility.currentViewWidth < DatePropertyDrawer.SingleLineWidth)
			{
				height += BeamGUI.StandardVerticalSpacing * 2;
			}
			else
			{
				height += BeamGUI.StandardVerticalSpacing;
			}
			
			// Add height for definitions list
			SerializedProperty definitionsProp = property.FindPropertyRelative("definitions");
			height += EditorGUI.GetPropertyHeight(definitionsProp, true);

			return height;
		}
	}
}
