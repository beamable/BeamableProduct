using System;
using Beamable.Common.Content;
using Beamable.Editor.Schedules;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Content
{

   [CustomPropertyDrawer(typeof(ListingSchedule))]
   public class ListingSchedulePropertyDrawer : SchedulePropertyDrawer<ListingSchedule, ListingScheduleWindow>
   {
   }

   [CustomPropertyDrawer(typeof(EventSchedule))]
   public class EventSchedulePropertyDrawer : SchedulePropertyDrawer<EventSchedule, EventScheduleWindow>
   {
   }

   public abstract class SchedulePropertyDrawer<TSchedule, TWindow> : PropertyDrawer
      where TSchedule : Schedule
      where TWindow : BeamableVisualElement, IScheduleWindow, new()
   {
      public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
      {
         return EditorGUIUtility.singleLineHeight * 4;
      }
      public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
      {

         EditorGUI.LabelField(position, label);

         var buttonRect = new Rect(position.x + 10, position.y + 20, position.width - 20, 20);

         var schedule = ContentRefPropertyDrawer.GetTargetObjectOfProperty(property) as Schedule;

         var requestEdit = GUI.Button(buttonRect, "Edit Schedule");
         var descRect = new Rect(buttonRect.x, buttonRect.y + 20, buttonRect.width, buttonRect.height);
         EditorGUI.SelectableLabel(descRect, property.FindPropertyRelative(nameof(Schedule.description)).stringValue);


         if (requestEdit)
         {
           OpenWindow(nextSchedule =>
           {
              schedule.description = nextSchedule.description;
              schedule.definitions = nextSchedule.definitions;
              schedule.activeFrom = nextSchedule.activeFrom;
              schedule.activeTo = nextSchedule.activeTo;
           });
         }
      }

      protected void OpenWindow(Action<Schedule> updater)
      {
         var element = new TWindow();
         var window = BeamablePopupWindow.ShowUtility(BeamableComponentsConstants.SCHEDULES_WINDOW_HEADER,
            element, null, BeamableComponentsConstants.ListingSchedulesWindowSize);
         element.OnScheduleUpdated += schedule =>
         {
            updater(schedule);
            window.Close();
         };
      }
   }
}