using Beamable.Common.Content;
using Beamable.Common.CronExpression;
using Beamable.CronExpression;
using Beamable.Editor.Util;
using Beamable.Editor.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Content
{
	[CustomPropertyDrawer(typeof(ScheduleDefinition))]
	public class ScheduleDefinitionPropertyDrawer : PropertyDrawer
	{
		private enum CronFieldType
		{
			Any,            // *
			Number,			// 0-99
			Between,        // X-Y
			EveryNth,       // */X
			EveryNthBetween, // X-Y/Z
			Custom          // Any cron value
		}

		private static readonly string[] FieldNames = {
			"second", "minute", "hour", "dayOfMonth", "month", "dayOfWeek", "year"
		};

		private static readonly string[] FieldLabels = {
			"Seconds", "Minutes", "Hours", "Day of Month", "Month", "Day of Week", "Year"
		};
		
		private readonly Dictionary<string,CronFieldType?[]> _propPathsCronTypes = new();
		private readonly Dictionary<string,string[]> _propPathCronParts = new();
		private readonly Dictionary<string,bool> _propPathsCronPartsFoldouts = new();
		
		private readonly Dictionary<string,string> _propPathRawCronFormat = new();
		private readonly Dictionary<string,string> _propPathHumanCronFormat = new();
		private readonly Dictionary<string, string> _propPathPreviousCronFormat = new();

		private bool TryGetPropertyValue<T>(Dictionary<string, T> baseDict, string propertyPath, out T value)
		{
			value = default;
			return baseDict.TryGetValue(propertyPath, out value);
		}

		private void SetPropertyValue<T>(Dictionary<string, T> baseDict, string propertyPath, T value)
		{
			baseDict[propertyPath] = value;
		}
		
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{

			var isEventContent = property.serializedObject.targetObject is EventContent;
			
			string propertyPath = property.propertyPath;
			if (TryGetPropertyValue(_propPathRawCronFormat, propertyPath,out string rawCronExpression) || string.IsNullOrEmpty(rawCronExpression))
			{
				InitCronParts(property); 
			}
			EditorGUI.BeginProperty(position, label, property);
			
			Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

			if (property.isExpanded)
			{
				EditorGUI.indentLevel++;

				float yOffset = BeamGUI.StandardVerticalSpacing;
				
				if (isEventContent)
				{
					Rect infoRect = new Rect(
						position.x,
						position.y + yOffset,
						position.width,
						EditorGUIUtility.singleLineHeight * 2
					);
    
					EditorGUI.HelpBox(
						infoRect, 
						"Event content cron schedule definitions uses the same second, minutes, and hours from Event Start Date. Therefore you cannot changes those cron fields.",
						MessageType.Info
					);
    
					yOffset += infoRect.height + EditorGUIUtility.standardVerticalSpacing;
				}

				Rect cronLabelRect = new Rect(position.x, position.y + yOffset, position.width,
				                              EditorGUIUtility.singleLineHeight);
				EditorGUI.LabelField(cronLabelRect, "Raw Cron Expression", EditorStyles.boldLabel);
				yOffset += BeamGUI.StandardVerticalSpacing;

				Rect cronValueRect = new Rect(position.x, position.y + yOffset, position.width,
				                              EditorGUIUtility.singleLineHeight);
				EditorGUI.LabelField(cronValueRect, new GUIContent(rawCronExpression));
				yOffset += BeamGUI.StandardVerticalSpacing;

				Rect humanLabelRect = new Rect(position.x, position.y + yOffset, position.width,
				                               EditorGUIUtility.singleLineHeight);
				EditorGUI.LabelField(humanLabelRect, "Human Readable", EditorStyles.boldLabel);
				yOffset += BeamGUI.StandardVerticalSpacing;

				Rect humanValueRect = new Rect(
					position.x, 
					position.y + yOffset, 
					position.width, 
					CalculateHumanCronExpressionHeight(propertyPath));
				
				GUIStyle wordWrapStyle = new GUIStyle(EditorStyles.label);
				wordWrapStyle.wordWrap = true;
				TryGetPropertyValue(_propPathHumanCronFormat, propertyPath, out string humanCronExpression);
				EditorGUI.LabelField(humanValueRect, new GUIContent(humanCronExpression), wordWrapStyle);
				yOffset += CalculateHumanCronExpressionHeight(propertyPath) + EditorGUIUtility.standardVerticalSpacing;

				Rect cronFoldoutRect = new Rect(
					position.x,
					position.y + yOffset,
					position.width,
					EditorGUIUtility.singleLineHeight);
				TryGetPropertyValue(_propPathsCronPartsFoldouts, propertyPath, out bool propPartFoldout);
				propPartFoldout = EditorGUI.Foldout(cronFoldoutRect, propPartFoldout, "Cron Values", true);
				SetPropertyValue(_propPathsCronPartsFoldouts, propertyPath, propPartFoldout);
				yOffset += BeamGUI.StandardVerticalSpacing;
				if (propPartFoldout)
				{
					EditorGUI.indentLevel++;

					for (int fieldIndex = 0; fieldIndex < FieldNames.Length; fieldIndex++)
					{
						// Event Schedule Definitions seconds, minutes, and hours are automatically set by the Event
						if(isEventContent && fieldIndex < 3)
							continue;
						yOffset = DrawField(position, yOffset, fieldIndex, propertyPath);
					}
					
					EditorGUI.indentLevel--;
				}

				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
			UpdateCronValue(property);
		}

		private float DrawField(Rect position, float yOffset, int fieldIndex, string propertyPath)
		{
			Rect labelField = new Rect(
				position.x,
				position.y + yOffset,
				position.width,
				EditorGUIUtility.singleLineHeight);

			EditorGUI.LabelField(labelField, FieldLabels[fieldIndex]);

			yOffset += BeamGUI.StandardVerticalSpacing;

			EditorGUI.indentLevel++;

			Rect typeRect = new Rect(
				position.x,
				position.y + yOffset,
				position.width,
				EditorGUIUtility.singleLineHeight);

			TryGetPropertyValue(_propPathsCronTypes, propertyPath, out CronFieldType?[] cronTypeParts);

			cronTypeParts[fieldIndex] = (CronFieldType)EditorGUI.EnumPopup(typeRect, "Type", cronTypeParts[fieldIndex]);

			yOffset += BeamGUI.StandardVerticalSpacing;
			
			switch (cronTypeParts[fieldIndex])
			{
				case CronFieldType.Any:
					UpdateCronPartValue("*", fieldIndex, propertyPath);
					break;

				case CronFieldType.Number:
					yOffset = DrawNumberField(position, yOffset, fieldIndex, propertyPath);
					break;

				case CronFieldType.Between:
					yOffset = DrawBetweenField(position, yOffset, fieldIndex, propertyPath);
					break;

				case CronFieldType.EveryNth:
					yOffset = DrawEveryNthField(position, yOffset, fieldIndex, propertyPath);
					break;

				case CronFieldType.EveryNthBetween:
					yOffset = DrawEveryNthBetweenField(position, yOffset, fieldIndex, propertyPath);
					break;

				case CronFieldType.Custom:
					yOffset = DrawCustomField(position, yOffset, fieldIndex, propertyPath);
					break;
			}
			
			EditorGUI.indentLevel--;


			return yOffset;
		}

		private float DrawNumberField(Rect position, float yOffset, int i, string propertyPath)
		{
			Rect customRect = new Rect(
				position.x,
				position.y + yOffset,
				position.width,
				EditorGUIUtility.singleLineHeight);
			int cronValue = 0;

			TryGetPropertyValue(_propPathCronParts, propertyPath, out string[] cronParts);
			
			if (int.TryParse(cronParts[i], out int intValue))
			{
				cronValue = intValue;
			}
			
			cronValue = EditorGUI.IntField(customRect, "Value", cronValue);

			UpdateCronPartValue($"{cronValue}", i, propertyPath);

			yOffset += BeamGUI.StandardVerticalSpacing;
			return yOffset;
		}
		
		private float DrawBetweenField(Rect position, float yOffset, int i, string propertyPath)
		{
			int start = 0;
			int end = 0;

			TryGetPropertyValue(_propPathCronParts, propertyPath, out string[] cronParts);
			
			if (cronParts[i].Contains("-"))
			{
				string[] valueParts = cronParts[i].Split('-');
				start = int.Parse(valueParts[0]);
				var endPart = valueParts[^1].Contains("/") ? valueParts[^1].Split("/")[0] : valueParts[^1];
				end = int.Parse(endPart);
			}
			
			var indentedRect = EditorGUI.IndentedRect(position);
			int oldIndentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			Rect lineRect = new Rect(indentedRect.x, indentedRect.y + yOffset, indentedRect.width, EditorGUIUtility.singleLineHeight);
			var lineRectController = new EditorGUIRectController(lineRect);
			var fieldsSize = lineRect.width / 2;
			
			var startRectController = new EditorGUIRectController(lineRectController.ReserveWidth(fieldsSize));
			var startLabel = new GUIContent("Start: ");
			GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleLeft
			};
			float startLabelSize = labelStyle.CalcSize(startLabel).x;
			EditorGUI.LabelField(startRectController.ReserveWidth(startLabelSize), startLabel);
			start = EditorGUI.IntField(startRectController.ReserveWidth(startRectController.rect.width - 2f), start);
			
			var endRectController = new EditorGUIRectController(lineRectController.ReserveWidth(fieldsSize));
			var endLabel = new GUIContent("End: ");
			float endLabelSize = labelStyle.CalcSize(endLabel).x;
			EditorGUI.LabelField(endRectController.ReserveWidth(endLabelSize), "End:");
			end = EditorGUI.IntField(endRectController.ReserveWidth(endRectController.rect.width - 2f), end);

			UpdateCronPartValue($"{start}-{end}", i, propertyPath);

			yOffset += BeamGUI.StandardVerticalSpacing;
			EditorGUI.indentLevel = oldIndentLevel;
			return yOffset;
		}
		
		private float DrawEveryNthField(Rect position, float yOffset, int i, string propertyPath)
		{
			Rect nthRect = new Rect(
				position.x,
				position.y + yOffset,
				position.width,
				EditorGUIUtility.singleLineHeight);

			TryGetPropertyValue(_propPathCronParts, propertyPath, out string[] cronParts);
			
			int nth = 1;
			if (cronParts[i].StartsWith("*/"))
			{
				int.TryParse(cronParts[i].Substring(2), out nth);
			}

			nth = EditorGUI.IntField(nthRect, "Every N", nth);
			UpdateCronPartValue($"*/{nth}", i, propertyPath);

			yOffset += BeamGUI.StandardVerticalSpacing;
			return yOffset;
		}

		private float DrawEveryNthBetweenField(Rect position, float yOffset, int i, string propertyPath)
		{
			int nthStart = 0;
			int nthEnd = 0;
			int step = 1;

			TryGetPropertyValue(_propPathCronParts, propertyPath, out string[] cronParts);
    
			if (cronParts[i].Contains("/") && cronParts[i].Contains("-"))
			{
				string[] rangeAndStep = cronParts[i].Split("/");
				if (rangeAndStep.Length == 2)
				{
					string[] rangeValues = rangeAndStep[0].Split('-');
					nthStart = int.Parse(rangeValues[0]);
					nthEnd = int.Parse(rangeValues[^1]);
				}
				step = int.Parse(rangeAndStep[^1]);
			}
			var indentedRect = EditorGUI.IndentedRect(position);
			int oldIndentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			Rect lineRect = new Rect(indentedRect.x, indentedRect.y + yOffset, indentedRect.width, EditorGUIUtility.singleLineHeight);
			var lineRectController = new EditorGUIRectController(lineRect);
			var fieldsSize = lineRect.width / 3;
			
			var startRectController = new EditorGUIRectController(lineRectController.ReserveWidth(fieldsSize));
			var startLabel = new GUIContent("Start: ");
			GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleLeft
			};
			float startLabelSize = labelStyle.CalcSize(startLabel).x;
			EditorGUI.LabelField(startRectController.ReserveWidth(startLabelSize), startLabel);
			nthStart = EditorGUI.IntField(startRectController.ReserveWidth(startRectController.rect.width - 2f), nthStart);
			
			var endRectController = new EditorGUIRectController(lineRectController.ReserveWidth(fieldsSize));
			var endLabel = new GUIContent("End: ");
			float endLabelSize = labelStyle.CalcSize(endLabel).x;
			EditorGUI.LabelField(endRectController.ReserveWidth(endLabelSize), "End:");
			nthEnd = EditorGUI.IntField(endRectController.ReserveWidth(endRectController.rect.width - 2f), nthEnd);
			
			var stepRectController = new EditorGUIRectController(lineRectController.ReserveWidth(fieldsSize));
			var stepLabel = new GUIContent("Step: ");
			float stepLabelSize = labelStyle.CalcSize(stepLabel).x;
			EditorGUI.LabelField(stepRectController.ReserveWidth(stepLabelSize), "Step:");
			step = EditorGUI.IntField(stepRectController.ReserveWidth(stepRectController.rect.width - 2f), step);

			UpdateCronPartValue($"{nthStart}-{nthEnd}/{step}", i, propertyPath);

			yOffset += BeamGUI.StandardVerticalSpacing;

			EditorGUI.indentLevel = oldIndentLevel;
			return yOffset;
		}

		

		private float DrawCustomField(Rect position, float yOffset, int i, string propertyPath)
		{
			Rect customRect = new Rect(
				position.x,
				position.y + yOffset,
				position.width,
				EditorGUIUtility.singleLineHeight);

			TryGetPropertyValue(_propPathCronParts, propertyPath, out string[] cronParts);
			
			string customValue = EditorGUI.TextField(customRect, "Value", cronParts[i]);

			UpdateCronPartValue(customValue, i, propertyPath);

			yOffset += BeamGUI.StandardVerticalSpacing;
			return yOffset;
		}

		private void InitCronParts(SerializedProperty property)
		{
			string propertyPropertyPath = property.propertyPath;
			if (!TryGetPropertyValue(_propPathsCronTypes, propertyPropertyPath, out CronFieldType?[] cronTypeParts))
			{
				cronTypeParts = new CronFieldType?[7];
			}

			if (!TryGetPropertyValue(_propPathCronParts, propertyPropertyPath, out string[] cronParts))
			{
				cronParts = new string[7];
			}
			
			for (int i = 0; i < FieldNames.Length; i++)
			{
				SerializedProperty fieldProp = property.FindPropertyRelative(FieldNames[i]);
				if (cronTypeParts[i].HasValue && !string.IsNullOrWhiteSpace(cronParts[i]))
				{
					continue;
				}

				List<string> currentValues = new List<string>();
				for (int j = 0; j < fieldProp.arraySize; j++)
				{
					currentValues.Add(fieldProp.GetArrayElementAtIndex(j).stringValue);
				}

				string partValue = ExpressionParser.ConvertToCronString(currentValues.ToArray());
				cronTypeParts[i] = GetFieldType(partValue);
				cronParts[i] = partValue;
			}
			
			SetPropertyValue(_propPathsCronTypes, propertyPropertyPath, cronTypeParts);
			SetPropertyValue(_propPathCronParts, propertyPropertyPath, cronParts);
			UpdateCronExpressions(propertyPropertyPath, cronParts);
			
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!property.isExpanded)
			{
				return EditorGUIUtility.singleLineHeight;
			}
			
			var isEventContent = property.serializedObject.targetObject is EventContent;

			string propertyPropertyPath = property.propertyPath;
			
			float height = BeamGUI.StandardVerticalSpacing * 5;

			if (isEventContent)
			{
				height += EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
			}
			
			height += CalculateHumanCronExpressionHeight(propertyPropertyPath);

			TryGetPropertyValue(_propPathsCronPartsFoldouts,  propertyPropertyPath, out bool cronPartsFoldout);
			
			if (!TryGetPropertyValue(_propPathsCronTypes, propertyPropertyPath, out CronFieldType?[] cronTypeParts))
			{
				cronTypeParts = new CronFieldType?[7];
			}
			
			if (cronPartsFoldout)
			{
				for (int i = 0; i < FieldNames.Length; i++)
				{
					if(isEventContent && i < 3)
						continue;
					
					height += BeamGUI.StandardVerticalSpacing;


					// Base height for type dropdown
					height += BeamGUI.StandardVerticalSpacing;

					switch (cronTypeParts[i])
					{
						case CronFieldType.Any:
							// No additional fields
							break;
						default:
							height += BeamGUI.StandardVerticalSpacing;
							break;
					}

				}
			}

			return height;
		}

		private CronFieldType GetFieldType(string partValue)
		{
			if (partValue == "*")
				return CronFieldType.Any;
            
			if (partValue.StartsWith("*/"))
				return CronFieldType.EveryNth;
            
			if (partValue.Contains("-") && partValue.Contains("/"))
				return CronFieldType.EveryNthBetween;
            
			if (partValue.Contains("-"))
				return CronFieldType.Between;
            
			return CronFieldType.Custom;
		}

		private void UpdateCronPartValue(string value, int cronPartIndex, string propertyPath)
		{
			TryGetPropertyValue(_propPathCronParts, propertyPath, out string[] cronParts);
			if (cronParts[cronPartIndex] != value)
			{
				cronParts[cronPartIndex] = value;
				SetPropertyValue(_propPathCronParts, propertyPath, cronParts);
				UpdateCronExpressions(propertyPath, cronParts);
			}
		}

		private void UpdateCronExpressions(string propertyPath, string[] cronParts)
		{
			var rawCronExpression = cronParts.Any(string.IsNullOrWhiteSpace) ? string.Empty : string.Join(" ", cronParts);
			string humanExpression = ExpressionDescriptor.GetDescription(rawCronExpression, out var errorData);
			if (errorData.IsError)
			{
				humanExpression = errorData.ErrorMessage;
				
			}

			SetPropertyValue(_propPathRawCronFormat, propertyPath, rawCronExpression);
			SetPropertyValue(_propPathHumanCronFormat,  propertyPath, humanExpression);
		}

		private void UpdateCronValue(SerializedProperty property)
		{
			TryGetPropertyValue(_propPathCronParts, property.propertyPath, out string[] cronParts);
			if (ContentRefPropertyDrawer.GetTargetObjectOfProperty(property) is ScheduleDefinition scheduleDefinition)
			{
				var rawCron =
					string.Join(" ", cronParts.ToList().Select(item => string.IsNullOrWhiteSpace(item) ? "*" : item));
				TryGetPropertyValue(_propPathPreviousCronFormat, property.propertyPath, out string oldRawCron);
				var scheduleCron = ExpressionParser.ScheduleDefinitionToCron(scheduleDefinition);
				if (oldRawCron == rawCron && rawCron == scheduleCron)
				{
					return;
				}
				SetPropertyValue(_propPathPreviousCronFormat, property.propertyPath, rawCron);

				scheduleDefinition.ApplyCronToScheduleDefinition(rawCron);
				property.serializedObject.ApplyModifiedProperties();
				Object targetObject = property.serializedObject.targetObject;
				EditorUtility.SetDirty(targetObject);
				if (targetObject is ContentObject contentObject)
				{
					contentObject.ForceValidate();
				}
			}
		}
		
		private float CalculateHumanCronExpressionHeight(string propertyPath)
		{
			TryGetPropertyValue(_propPathHumanCronFormat, propertyPath, out string humanCronExpression);
			
			if (string.IsNullOrEmpty(humanCronExpression))
				return EditorGUIUtility.singleLineHeight;
			
			Rect tempRect = new Rect(0, 0, EditorGUIUtility.currentViewWidth, 0);
			float margin = 5f; 
			float availableWidth = tempRect.width - 15f - margin * 2;
			GUIStyle style = EditorStyles.label;
			style.wordWrap = true;
			return style.CalcHeight(new GUIContent(humanCronExpression), availableWidth);
		}
	}
}
