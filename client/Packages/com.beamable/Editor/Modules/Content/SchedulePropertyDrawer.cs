using Beamable.Common.Content;
using Beamable.Common.Shop;
using Beamable.CronExpression;
using Beamable.Editor.Schedules;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using System;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Content
{

	[CustomPropertyDrawer(typeof(ListingSchedule))]
	public class ListingSchedulePropertyDrawer : SchedulePropertyDrawer<ListingContent, ListingScheduleWindow>
	{
		protected override ListingContent GetDataObject(SerializedProperty property)
		{
			return ContentRefPropertyDrawer.GetTargetParentObjectOfProperty(property, 2) as ListingContent;
		}
	}

	[CustomPropertyDrawer(typeof(EventSchedule))]
	public class EventSchedulePropertyDrawer : SchedulePropertyDrawer<EventContent, EventScheduleWindow>
	{
		protected override EventContent GetDataObject(SerializedProperty property)
		{
			return ContentRefPropertyDrawer.GetTargetParentObjectOfProperty(property, 2) as EventContent;
		}

		protected override void UpdateSchedule(SerializedProperty property, EventContent evtContent, Schedule schedule, Schedule nextSchedule)
		{
			base.UpdateSchedule(property, evtContent, schedule, nextSchedule);
			var startDate = DateTime.Parse(evtContent.startDate);
			startDate = startDate.ToUniversalTime();
			schedule.activeFrom = $"{startDate.Year:0000}-{startDate.Month:00}-{startDate.Day:00}T{schedule.activeFrom}";
			evtContent.startDate = schedule.activeFrom;
		}
	}


	public abstract class SchedulePropertyDrawer<TData, TWindow> : PropertyDrawer

	   where TWindow : BeamableVisualElement, IScheduleWindow<TData>, new()
	{
		public bool allowRawEdit;
		private Schedule _schedule;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight * 4 +
				   EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(Schedule.description))) + 2 +
				   EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(Schedule.definitions))) + 2 +
				   EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(Schedule.activeTo))) + 2 +
				   EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(Schedule.activeFrom)));
		}
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var topRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.LabelField(topRect, label);

			var indent = 20;

			var buttonRect = new Rect(position.x + indent, position.y + 20, position.width - indent * 2, 20);

			_schedule = ContentRefPropertyDrawer.GetTargetObjectOfProperty(property) as Schedule;

			var requestEdit = GUI.Button(buttonRect, "Edit Schedule");

			var nextY = buttonRect.y + 20;
			buttonRect = new Rect(buttonRect.x, nextY, buttonRect.width, 20);
			var toggleRaw = GUI.Button(buttonRect, "Modify Raw Data");
			if (toggleRaw)
				allowRawEdit = !allowRawEdit;
			nextY = buttonRect.y + 20;

			for (var index = 0; index < _schedule.definitions.Count; index++)
			{
				var scheduleDefinition = _schedule.definitions[index];
				scheduleDefinition.index = index;
				scheduleDefinition.OnCronRawSaveButtonPressed -= HandleCronRawUpdate;
				scheduleDefinition.OnCronRawSaveButtonPressed += HandleCronRawUpdate;
			}

			void RenderProperty(SerializedProperty prop)
			{
				var height = EditorGUI.GetPropertyHeight(prop);
				var rect = new Rect(buttonRect.x, nextY, buttonRect.width, height);
				nextY += height + 2;
				EditorGUI.PropertyField(rect, prop, true);
			}

			GUI.enabled = allowRawEdit;
			RenderProperty(property.FindPropertyRelative(nameof(Schedule.description)));
			RenderProperty(property.FindPropertyRelative(nameof(Schedule.activeFrom)));
			RenderProperty(property.FindPropertyRelative(nameof(Schedule.activeTo)));
			RenderProperty(property.FindPropertyRelative(nameof(Schedule.definitions)));
			GUI.enabled = true;

			if (requestEdit)
			{
				OpenWindow(property, _schedule);
			}
		}

		protected void OpenWindow(SerializedProperty property, Schedule schedule)
		{
			var element = new TWindow();
			var window = BeamablePopupWindow.ShowUtility(BeamableComponentsConstants.SCHEDULES_WINDOW_HEADER,
			   element, null, BeamableComponentsConstants.SchedulesWindowSize);


			var data = GetDataObject(property);
			if (data == null)
			{
				Debug.LogWarning("No data object exists for " + property);
			}

			element.Set(schedule, data);
			element.OnScheduleUpdated += nextSchedule =>
			{
				element.ApplyDataTransforms(data);
				UpdateSchedule(property, data, schedule, nextSchedule);
				window.Close();
			};
			element.OnCancelled += () => window.Close();

		}

		private void HandleCronRawUpdate(ScheduleDefinition scheduleDefinition)
		{
			var newDefinition = ExpressionDescriptor.CronToScheduleDefinition(scheduleDefinition.cronRawFormat);
			newDefinition.cronRawFormat = scheduleDefinition.cronRawFormat;
			newDefinition.cronHumanFormat = ExpressionDescriptor.GetDescription(newDefinition.cronRawFormat, out _);
			_schedule.definitions[scheduleDefinition.index] = newDefinition;
		}

		protected abstract TData GetDataObject(SerializedProperty property);

		protected virtual void UpdateSchedule(SerializedProperty property, TData data, Schedule schedule, Schedule nextSchedule)
		{
			schedule.description = nextSchedule.description;
			schedule.activeFrom = nextSchedule.activeFrom;
			schedule.activeTo = nextSchedule.activeTo;
			schedule.definitions = nextSchedule.definitions;
			SetDefinitions(schedule);
		}

		private void SetDefinitions(Schedule schedule)
		{
			foreach (var definition in schedule.definitions)
			{
				definition.cronRawFormat = ExpressionDescriptor.ScheduleDefinitionToCron(definition);
				definition.cronHumanFormat = ExpressionDescriptor.GetDescription(definition.cronRawFormat, out _);
			}
		}
	}
}
