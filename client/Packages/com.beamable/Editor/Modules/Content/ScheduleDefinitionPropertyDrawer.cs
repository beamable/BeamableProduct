using Beamable.Common.Content;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Content
{
	[CustomPropertyDrawer(typeof(ScheduleDefinition))]
	public class ScheduleDefinitionPropertyDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property);
		}
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var rectController = new EditorGUIRectController(position);

			EditorGUI.LabelField(rectController.ReserveSingleLine(), label);
			EditorGUI.PropertyField(position, property, true);

			if (!property.isExpanded)
				return;

			rectController.ReserveSingleLine();
			rectController.rect.height = 18f;
			rectController.rect.y += 2f;

			if (GUI.Button(rectController.ReserveWidthFromRight(50), "Edit"))
			{
				var definition = ContentRefPropertyDrawer.GetTargetObjectOfProperty(property) as ScheduleDefinition;
				HandleEditRawCronButton(definition);
			}
		}

		private void HandleEditRawCronButton(ScheduleDefinition definition)
		{
			CronEditorWindow.ShowWindow(definition.cronRawFormat, result =>
			{
				definition.cronRawFormat = result;
				definition.OnCronRawSaveButtonPressed?.Invoke(definition);
			});
		}
	}
}
