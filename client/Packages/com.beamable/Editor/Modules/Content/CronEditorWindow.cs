using System;
using Beamable.CronExpression;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Content
{
    public class CronEditorWindow : EditorWindow
    {
        public static CronEditorWindow ShowWindow(string cronRawFormat, Action<string> result) 
        {
            _window = GetWindow<CronEditorWindow>();
            if (_window != null)
                _window.Close();
            
            _window = CreateInstance<CronEditorWindow>();
            _window.titleContent = new GUIContent("Cron Editor");
            _window.minSize = _window.maxSize = new Vector2(400f, 300f);
            _window.ShowUtility();
            _window.Init(cronRawFormat, result);
            
            return _window;
        }

        private static CronEditorWindow _window;
        private static Action<string> _result;
        private static string _currentString = "-1";
        private static string _inputString = string.Empty;
        private static string _humanFormat = string.Empty;
        private ErrorData _errorData;

        private void Init(string cronRawFormat, Action<string> result)
        {
            _inputString = cronRawFormat;
            _result = result;
            _errorData = new ErrorData();
        }

        private void OnGUI() 
        {
            GUI.Label(new Rect(10, 190, 380, 20), "Enter cron string", new GUIStyle(GUI.skin.GetStyle("label"))
            {
	            alignment = TextAnchor.MiddleLeft
            });
            
            _inputString = GUI.TextField(new Rect(10, 210, 380, 48), _inputString, new GUIStyle(GUI.skin.GetStyle("textField"))
            {
	            wordWrap = true
            });
            
            if (!_inputString.Equals(_currentString))
            {
                _humanFormat = ExpressionDescriptor.GetDescription(_inputString, out _errorData);
                _currentString = _inputString;
            }

            GUI.Label(new Rect(10, 10, 380, 100), _humanFormat, new GUIStyle(GUI.skin.GetStyle("label"))
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            });

            if (GUI.Button(new Rect(260, 270, 60, 20), "Cancel"))
            {
                _window.Close();
            }

            GUI.enabled = !_errorData.IsError;
            if (GUI.Button(new Rect(330, 270, 60, 20), "Save"))
            {
	            _result?.Invoke(_currentString.TrimEnd());
                _window.Close();
            }
            GUI.enabled = true;
        }
    }
}
