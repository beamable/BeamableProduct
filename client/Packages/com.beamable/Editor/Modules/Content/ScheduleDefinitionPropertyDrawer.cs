using Beamable.Common.Content;
using Beamable.CronExpression;
using Editor.Utility;
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

		private static readonly float StandardVerticalSpacing = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

		private readonly bool[] _foldouts = new bool[7];
		private readonly CronFieldType?[] _cronTypeParts = new CronFieldType?[7];
		private readonly string[] _cronParts = new string[7];
		private bool _cronPartsFoldout;
		
		private string _rawCronExpression;
		private string _humanCronExpression;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (string.IsNullOrEmpty(_rawCronExpression))
			{
				InitCronParts(property); 
			}
			EditorGUI.BeginProperty(position, label, property);
			
			Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

			if (property.isExpanded)
			{
				EditorGUI.indentLevel++;

				float yOffset = StandardVerticalSpacing;

				Rect cronLabelRect = new Rect(position.x, position.y + yOffset, position.width,
				                              EditorGUIUtility.singleLineHeight);
				EditorGUI.LabelField(cronLabelRect, "Raw Cron Expression", EditorStyles.boldLabel);
				yOffset += StandardVerticalSpacing;

				Rect cronValueRect = new Rect(position.x, position.y + yOffset, position.width,
				                              EditorGUIUtility.singleLineHeight);
				EditorGUI.LabelField(cronValueRect, new GUIContent(_rawCronExpression));
				yOffset += StandardVerticalSpacing;

				Rect humanLabelRect = new Rect(position.x, position.y + yOffset, position.width,
				                               EditorGUIUtility.singleLineHeight);
				EditorGUI.LabelField(humanLabelRect, "Human Readable", EditorStyles.boldLabel);
				yOffset += StandardVerticalSpacing;

				Rect humanValueRect = new Rect(position.x, position.y + yOffset, position.width,
				                               EditorGUIUtility.singleLineHeight);
				
				EditorGUI.LabelField(humanValueRect, new GUIContent(_humanCronExpression));
				yOffset += StandardVerticalSpacing;

				Rect cronFoldoutRect = new Rect(
					position.x,
					position.y + yOffset,
					position.width,
					EditorGUIUtility.singleLineHeight);
				
				_cronPartsFoldout = EditorGUI.Foldout(cronFoldoutRect, _cronPartsFoldout, "Cron Values", true);
				yOffset += StandardVerticalSpacing;
				if (_cronPartsFoldout)
				{
					EditorGUI.indentLevel++;

					for (int fieldIndex = 0; fieldIndex < FieldNames.Length; fieldIndex++)
					{
						yOffset = DrawField(position, yOffset, fieldIndex);
					}
					
					Rect buttonRect = new Rect(
						position.x,
						position.y + yOffset,
						position.width,
						EditorGUIUtility.singleLineHeight);
					if (GUI.Button(buttonRect, "Apply Changes"))
					{
						UpdateCronValue(property);
					}
					
					EditorGUI.indentLevel--;
				}

				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
		}

		private float DrawField(Rect position, float yOffset, int fieldIndex)
		{
			Rect foldoutFieldRect = new Rect(
				position.x,
				position.y + yOffset,
				position.width,
				EditorGUIUtility.singleLineHeight);

			_foldouts[fieldIndex] = EditorGUI.Foldout(foldoutFieldRect, _foldouts[fieldIndex], FieldLabels[fieldIndex], true);
						
			yOffset += StandardVerticalSpacing;

			if (_foldouts[fieldIndex])
			{
				EditorGUI.indentLevel++;

				Rect typeRect = new Rect(
					position.x,
					position.y + yOffset,
					position.width,
					EditorGUIUtility.singleLineHeight);
							
				_cronTypeParts[fieldIndex] = (CronFieldType)EditorGUI.EnumPopup(typeRect, "Type", _cronTypeParts[fieldIndex]);

				yOffset += StandardVerticalSpacing;

				switch (_cronTypeParts[fieldIndex])
				{
					case CronFieldType.Any:
						UpdateCronPartValue("*", fieldIndex);
						break;
								
					case CronFieldType.Number:
						yOffset = DrawNumberField(position, yOffset, fieldIndex);
						break;

					case CronFieldType.Between:
						yOffset = DrawBetweenField(position, yOffset, fieldIndex);
						break;

					case CronFieldType.EveryNth:
						yOffset = DrawEveryNthField(position, yOffset, fieldIndex);
						break;

					case CronFieldType.EveryNthBetween:
						yOffset = DrawEveryNthBetweenField(position, yOffset, fieldIndex);
						break;

					case CronFieldType.Custom:
						yOffset = DrawCustomField(position, yOffset, fieldIndex);
						break;
				}

				EditorGUI.indentLevel--;
			}

			return yOffset;
		}

		private float DrawNumberField(Rect position, float yOffset, int i)
		{
			Rect customRect = new Rect(
				position.x,
				position.y + yOffset,
				position.width,
				EditorGUIUtility.singleLineHeight);
			int cronValue = 0;
			if (int.TryParse(_cronParts[i], out int intValue))
			{
				cronValue = intValue;
			}
			cronValue = EditorGUI.IntField(customRect, "Value", cronValue);

			UpdateCronPartValue($"{cronValue}", i);

			yOffset += StandardVerticalSpacing;
			return yOffset;
		}
		
		private float DrawBetweenField(Rect position, float yOffset, int i)
		{
			Rect startRect = new Rect(
				position.x,
				position.y + yOffset,
				position.width,
				EditorGUIUtility.singleLineHeight);

			Rect endRect = new Rect(
				position.x,
				position.y + yOffset + EditorGUIUtility.singleLineHeight +
				EditorGUIUtility.standardVerticalSpacing,
				position.width,
				EditorGUIUtility.singleLineHeight);

			int start = 0;
			int end = 0;

			if (_cronParts[i].Contains("-"))
			{
				string[] valueParts = _cronParts[i].Split('-');
				start = int.Parse(valueParts[0]);
				end = int.Parse(valueParts[^1]);
			}

			start = EditorGUI.IntField(startRect, "Start", start);
			end = EditorGUI.IntField(endRect, "End", end);

			UpdateCronPartValue($"{start}-{end}", i);

			yOffset += StandardVerticalSpacing * 2;
			return yOffset;
		}
		
		private float DrawEveryNthField(Rect position, float yOffset, int i)
		{
			Rect nthRect = new Rect(
				position.x,
				position.y + yOffset,
				position.width,
				EditorGUIUtility.singleLineHeight);

			int nth = 1;
			if (_cronParts[i].StartsWith("*/"))
			{
				int.TryParse(_cronParts[i].Substring(2), out nth);
			}

			nth = EditorGUI.IntField(nthRect, "Every N", nth);
			UpdateCronPartValue($"*/{nth}", i);

			yOffset += StandardVerticalSpacing;
			return yOffset;
		}

		private float DrawEveryNthBetweenField(Rect position, float yOffset, int i)
		{
			Rect nthStartRect = new Rect(
				position.x,
				position.y + yOffset,
				position.width,
				EditorGUIUtility.singleLineHeight);

			Rect nthEndRect = new Rect(
				position.x,
				position.y + yOffset + EditorGUIUtility.singleLineHeight +
				EditorGUIUtility.standardVerticalSpacing,
				position.width,
				EditorGUIUtility.singleLineHeight);

			Rect stepRect = new Rect(
				position.x,
				position.y + yOffset + StandardVerticalSpacing * 2,
				position.width,
				EditorGUIUtility.singleLineHeight);

			int nthStart = 0;
			int nthEnd = 0;
			int step = 1;

			if (_cronParts[i].Contains("/") && _cronParts[i].Contains("-"))
			{
				string[] rangeAndStep = _cronParts[i].Split("/");
				if (rangeAndStep.Length == 2)
				{
					string[] rangeValues = rangeAndStep[0].Split('-');
					nthStart = int.Parse(rangeValues[0]);
					nthEnd = int.Parse(rangeValues[^1]);
				}
				step = int.Parse(rangeAndStep[^1]);
			}

			nthStart = EditorGUI.IntField(nthStartRect, "Start", nthStart);
			nthEnd = EditorGUI.IntField(nthEndRect, "End", nthEnd);
			step = EditorGUI.IntField(stepRect, "Step", step);

			UpdateCronPartValue($"{nthStart}-{nthEnd}/{step}", i);

			yOffset += StandardVerticalSpacing * 3;
			return yOffset;
		}

		

		private float DrawCustomField(Rect position, float yOffset, int i)
		{
			Rect customRect = new Rect(
				position.x,
				position.y + yOffset,
				position.width,
				EditorGUIUtility.singleLineHeight);

			string customValue = EditorGUI.TextField(customRect, "Value", _cronParts[i]);

			UpdateCronPartValue(customValue, i);

			yOffset += StandardVerticalSpacing;
			return yOffset;
		}

		private void InitCronParts(SerializedProperty property)
		{
			for (int i = 0; i < FieldNames.Length; i++)
			{
				SerializedProperty fieldProp = property.FindPropertyRelative(FieldNames[i]);
				if (_cronTypeParts[i].HasValue && !string.IsNullOrWhiteSpace(_cronParts[i]))
				{
					continue;
				}

				List<string> currentValues = new List<string>();
				for (int j = 0; j < fieldProp.arraySize; j++)
				{
					currentValues.Add(fieldProp.GetArrayElementAtIndex(j).stringValue);
				}

				string partValue = string.Join("-", currentValues);
				_cronTypeParts[i] = GetFieldType(partValue);
				_cronParts[i] = partValue;
			}
			UpdateCronExpressions();
			
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!property.isExpanded)
			{
				return EditorGUIUtility.singleLineHeight;
			}

			float height = StandardVerticalSpacing * 6;

			if (_cronPartsFoldout)
			{
				for (int i = 0; i < FieldNames.Length; i++)
				{
					height += StandardVerticalSpacing;

					if (_foldouts[i])
					{
						// Base height for type dropdown
						height += StandardVerticalSpacing;
						
						switch (_cronTypeParts[i])
						{
							case CronFieldType.Any:
								// No additional fields
								break;
							case CronFieldType.Between:
								height += StandardVerticalSpacing * 2;
								break;
							case CronFieldType.EveryNth:
								height += StandardVerticalSpacing;
								break;
							case CronFieldType.EveryNthBetween:
								height += StandardVerticalSpacing * 3;
								break;
							case CronFieldType.Custom:
								height += StandardVerticalSpacing;
								break;
						}
					}
				}
				height += StandardVerticalSpacing; //  Button
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

		private void UpdateCronPartValue(string value, int cronPartIndex)
		{
			if (_cronParts[cronPartIndex] != value)
			{
				_cronParts[cronPartIndex] = value;
				UpdateCronExpressions();
			}
		}

		private void UpdateCronExpressions()
		{
			_rawCronExpression = _cronParts.Any(string.IsNullOrWhiteSpace) ? string.Empty : string.Join(" ", _cronParts);
			string humanExpression = ExpressionDescriptor.GetDescription(_rawCronExpression, out var errorData);
			if (errorData.IsError)
				humanExpression = errorData.ErrorMessage;
			_humanCronExpression = humanExpression;
		}

		private void UpdateCronValue(SerializedProperty property)
		{
			if (ContentRefPropertyDrawer.GetTargetObjectOfProperty(property) is ScheduleDefinition scheduleDefinition)
			{
				var rawCron =
					string.Join(" ", _cronParts.ToList().Select(item => string.IsNullOrWhiteSpace(item) ? "*" : item));
				scheduleDefinition.ApplyCronToScheduleDefinition(rawCron);
				Object targetObject = property.serializedObject.targetObject;
				EditorUtility.SetDirty(targetObject);
				if (targetObject is ContentObject contentObject)
				{
					contentObject.ForceValidate();
				}
			}
		}
	}
}
