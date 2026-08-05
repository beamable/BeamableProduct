using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.Editor.Util
{
	public partial class BeamGUI
	{
		private static GUIStyle placeholderStyle;

		public static string PlaceholderPasswordField(Rect rect,
		                                              string text,
		                                              string placeholder,
		                                              GUIStyle styles,
		                                              GUIStyle labelStyle = null)
		{
			var topRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
			var nextText = EditorGUI.PasswordField(rect, text, styles);
			if (string.IsNullOrEmpty(text))
			{
				if (placeholderStyle == null)
				{
					placeholderStyle = new GUIStyle(EditorStyles.label)
					{
						padding = new RectOffset(4, 0, 0, 0),
						normal = new GUIStyleState
						{
							textColor = Color.Lerp(EditorStyles.label.normal.textColor, new Color(1, 1, 1, 0f),
							                       .5f)
						}
					};
				}

				EditorGUI.LabelField(topRect, placeholder, labelStyle ?? placeholderStyle);
			}

			return nextText;
		}

		public static string PlaceholderTextField(Rect rect,
		                                          string text,
		                                          string placeholder,
		                                          GUIStyle styles,
		                                          GUIStyle labelStyle = null)
		{
			var topRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
			var nextText = EditorGUI.TextField(rect, text, styles);
			if (string.IsNullOrEmpty(text))
			{
				if (placeholderStyle == null)
				{
					placeholderStyle = new GUIStyle(EditorStyles.label)
					{
						padding = new RectOffset(4, 0, 0, 0),
						normal = new GUIStyleState
						{
							textColor = Color.Lerp(EditorStyles.label.normal.textColor, new Color(1, 1, 1, 0f),
							                       .5f)
						}
					};
				}

				EditorGUI.LabelField(topRect, placeholder, labelStyle ?? placeholderStyle);
			}

			return nextText;
		}
		/// <summary>
    /// Event-based overload. Subscribe to <paramref name="onOptionSelected"/>
    /// to receive the value the user picks from the dropdown.
    /// The return value reflects live typing only.
    /// </summary>
    public static string DropdownTextField(
        Rect rect,
        string text,
        string placeholder,
        string[] options,
        System.Action<string> onOptionSelected,
        GUIStyle fieldStyle = null,
        GUIStyle labelStyle = null)
    {
        fieldStyle ??= EditorStyles.textField;

        const float buttonWidth = 25f;
        const float spacing    = 2f;

        var fieldWidth = options != null && options.Length > 0 ? rect.width - buttonWidth - spacing : rect.width;
        var fieldRect  = new Rect(rect.x, rect.y, fieldWidth, rect.height);
        var buttonRect = new Rect(fieldRect.xMax + spacing, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);

        // ── Text field ──────────────────────────────────────────────────
        var nextText = EditorGUI.TextField(fieldRect, text, fieldStyle);

        // ── Placeholder ─────────────────────────────────────────────────
        if (string.IsNullOrEmpty(text))
        {
            if (placeholderStyle == null)
            {
	            placeholderStyle = new GUIStyle(EditorStyles.label)
                {
                    padding = new RectOffset(4, 0, 0, 0),
                    normal  = new GUIStyleState
                    {
                        textColor = Color.Lerp(
                            EditorStyles.label.normal.textColor,
                            new Color(1, 1, 1, 0f),
                            0.5f)
                    }
                };
            }

            var placeholderRect = new Rect(fieldRect.x, fieldRect.y, fieldRect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(placeholderRect, placeholder, labelStyle ?? placeholderStyle);
        }

        var buttonStyle = EditorStyles.miniButton;
        buttonStyle.stretchHeight = true;
        buttonStyle.fixedHeight = rect.height;
        // ── Dropdown button ─────────────────────────────────────────────
        if (options != null && options.Length > 0 && GUI.Button(buttonRect, "▼", buttonStyle))
        {
            var menuContent = new GUIContent[options.Length];
            var selected    = -1;

            for (int i = 0; i < options.Length; i++)
            {
                menuContent[i] = new GUIContent(options[i]);
                if (options[i] == text)
                {
	                selected = i;
                }
            }

            EditorUtility.DisplayCustomMenu(
                new Rect(buttonRect.x, buttonRect.yMax, 0, 0),
                menuContent,
                selected,
                (userData, opts, index) =>
                {
                    var callback = (System.Action<string>)userData;
                    callback?.Invoke(opts[index]);
                },
                onOptionSelected,
                false);
        }

        return nextText;
    }

		public static string LayoutPlaceholderTextField(string text,
		                                                string placeholder,
		                                                GUIStyle styles,
		                                                params GUILayoutOption[] options)
		{
			var rect = GUILayoutUtility.GetRect(new GUIContent(text), styles, options);
			return PlaceholderTextField(rect, text, placeholder, styles);
		}
	}
}
