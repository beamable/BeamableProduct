using Beamable.Common.CronExpression;
using Beamable.CronExpression;
using Beamable.Editor.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.CronEditorWindow
{
	public class CronEditorWindow : EditorWindow
	{

		private string _cronExpression = "";
		private string _validationMessage = "";
		private bool _isValid;
		private Vector2 _horizontalScrollPosition;
		private Vector2 _verticalScrollPosition;
		private bool _showExamples;
	

		[MenuItem("Tools/Cron Window Editor")]
		public static void ShowWindow()
		{
			CreateWindow<CronEditorWindow>("Cron Validator");
		}
	

		private void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();
			_horizontalScrollPosition = EditorGUILayout.BeginScrollView(_horizontalScrollPosition);
			EditorGUILayout.BeginVertical();
			{
				_verticalScrollPosition = EditorGUILayout.BeginScrollView(_verticalScrollPosition);
				
				EditorGUILayout.LabelField("Cron Expression Validator", EditorStyles.boldLabel);
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Cron Expression Pattern:", EditorStyles.boldLabel);
				var cronPatternContent = new GUIContent("[SECONDS MINUTES HOURS DAY-OF-MONTH MONTH DAY-OF-WEEK YEARS]");
				float contentSize = EditorStyles.boldLabel.CalcSize(cronPatternContent).x;
				EditorGUILayout.LabelField(cronPatternContent, EditorStyles.boldLabel, GUILayout.Width(contentSize));
				
				
				EditorGUI.BeginChangeCheck();
				_cronExpression = EditorGUILayout.TextField("Cron Expression", _cronExpression);
				if (EditorGUI.EndChangeCheck())
				{
					ValidateCronExpression();
				}

			
				EditorGUILayout.Space();
				var messageStyle = new GUIStyle(EditorStyles.helpBox);
				messageStyle.normal.textColor = _isValid ? Color.green : Color.red;
				EditorGUILayout.LabelField(_validationMessage, messageStyle);
		

				if (GUILayout.Button("Clear"))
				{
					_cronExpression = "";
					_validationMessage = "";
					_isValid = false;
				}

			

			
				_showExamples = EditorGUILayout.Foldout(_showExamples, "Cron Expression Examples", true);
				if (_showExamples)
				{
					DrawExamples();
				}

				EditorGUILayout.EndScrollView();
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndHorizontal();
		}

		private void ValidateCronExpression()
		{
			if (string.IsNullOrWhiteSpace(_cronExpression))
			{
				_validationMessage = "Cron Expression is empty, please enter a cron expression";
				_isValid = false;
				return;
			}

			_validationMessage = ExpressionDescriptor.GetDescription(_cronExpression,
			                                                        new Options() {Locale = CronLocale.en_US},
			                                                        out ErrorData errorData);
			if (errorData.IsError)
			{
				_validationMessage = errorData.ErrorMessage;
				_isValid = false;
				return;
			}

			_isValid = true;
		}

		private void DrawExamples()
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Common Examples:", EditorStyles.boldLabel);

			DrawExample("Every second", "* * * * * * *");
			DrawExample("Every minute", "0 * * * * * *");
			DrawExample("Every hour", "0 0 * * * * *");
			DrawExample("At Midnight", "0 0 0 * * * *");
			DrawExample("Every weekday at 9:30 AM", "0 30 9 * * 1-5 *");
			DrawExample("Yearly on Jan 1 at midnight", "0 0 0 1 1 * *");
			DrawExample("Every 5 seconds", "*/5 * * * * * *");
			DrawExample("Every 15 minutes, 2023 through 2025", "0 */15 * * * * 2023-2025");
		}

		private void DrawExample(string description, string example)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(description, GUILayout.Width(180));
			if (GUILayout.Button(example, EditorStyles.miniButton))
			{
				_cronExpression = example;
				ValidateCronExpression();
				GUI.FocusControl(null);
			}

			EditorGUILayout.EndHorizontal();
		}
	}
}
