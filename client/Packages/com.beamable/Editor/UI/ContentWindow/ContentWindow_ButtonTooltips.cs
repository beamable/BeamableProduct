using Beamable.Editor.Util;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		private string _buttonTooltipCandidate;
		private string _buttonTooltipText;
		private Rect _buttonTooltipCandidateRect;
		private Rect _buttonTooltipAnchor;
		private bool _buttonTooltipAbove;
		private bool _buttonTooltipVisible;
		private double _buttonTooltipShowAt;
		private GUIStyle _buttonTooltipStyle;

		private bool DrawHeaderButtonWithTooltip(string label, Texture icon, string tooltip, Texture badge = null)
		{
			var rect = GUILayoutUtility.GetRect(
				GUIContent.none,
				GUIStyle.none,
				GUILayout.Width(HEADER_BUTTON_WIDTH),
				GUILayout.ExpandHeight(true));

			RegisterButtonTooltip(rect, tooltip);

			bool clicked = BeamGUI.HeaderButton(
				label,
				icon,
				width: HEADER_BUTTON_WIDTH,
				iconPadding: 2,
				forcedRect: rect);

			if (badge != null)
			{
				const float badgeSize = 14f;

				var badgeRect = new Rect(
					rect.xMax - badgeSize - 3f,
					rect.yMin + 2f,
					badgeSize,
					badgeSize);

				GUI.DrawTexture(
					badgeRect,
					badge,
					ScaleMode.ScaleToFit,
					true);
			}

			return clicked;
		}

		private void BeginButtonTooltipFrame()
		{
			if (Event.current.type == EventType.Repaint)
				_buttonTooltipCandidate = null;

			if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseLeaveWindow ||
			    Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag ||
			    Event.current.type == EventType.ScrollWheel)
			{
				ResetButtonTooltip();
				Repaint();
			}
		}

		private void ResetButtonTooltip()
		{
			_buttonTooltipCandidate = null;
			_buttonTooltipText = null;
			_buttonTooltipVisible = false;
		}

		private void RegisterButtonTooltip(Rect rect, string text, bool above = false)
		{
			// Register disabled controls too, so Publish can explain why it is unavailable.
			if (Event.current.type != EventType.Repaint || mouseOverWindow != this ||
			    GUIUtility.hotControl != 0 || string.IsNullOrEmpty(text) || !rect.Contains(Event.current.mousePosition))
				return;

			_buttonTooltipCandidate = text;
			_buttonTooltipCandidateRect = GUIUtility.GUIToScreenRect(rect);
			_buttonTooltipAbove = above;
		}

		private void DrawButtonTooltip()
		{
			if (Event.current.type != EventType.Repaint)
				return;

			if (_buttonTooltipCandidate == null)
			{
				ResetButtonTooltip();
				return;
			}

			if (_buttonTooltipText != _buttonTooltipCandidate || _buttonTooltipAnchor != _buttonTooltipCandidateRect)
			{
				_buttonTooltipText = _buttonTooltipCandidate;
				_buttonTooltipAnchor = _buttonTooltipCandidateRect;
				_buttonTooltipShowAt = EditorApplication.timeSinceStartup + 0.4;
				_buttonTooltipVisible = false;
			}
			if (EditorApplication.timeSinceStartup < _buttonTooltipShowAt)
				return;

			_buttonTooltipVisible = true;
			_buttonTooltipStyle ??= new GUIStyle(EditorStyles.helpBox)
			{
				wordWrap = true,
				fontSize = EditorStyles.label.fontSize,
				padding = new RectOffset(8, 8, 5, 5)
			};
			var content = new GUIContent(_buttonTooltipText);
			float width = Mathf.Min(320, Mathf.Max(1, position.width - 8), _buttonTooltipStyle.CalcSize(content).x);
			float height = _buttonTooltipStyle.CalcHeight(content, width);
			var anchor = new Rect(GUIUtility.ScreenToGUIPoint(_buttonTooltipAnchor.position), _buttonTooltipAnchor.size);
			var bounds = new Rect(4, 4, Mathf.Max(1, position.width - 8), Mathf.Max(1, position.height - 8));
			var tooltipRect = GetButtonTooltipRect(bounds, anchor, new Vector2(width, height), _buttonTooltipAbove);

			// Draw after the panels, outside their scroll views, with an opaque background.
			EditorGUI.DrawRect(tooltipRect, EditorGUIUtility.isProSkin
				? new Color(0.16f, 0.16f, 0.16f) : new Color(0.94f, 0.94f, 0.94f));
			GUI.Label(tooltipRect, content, _buttonTooltipStyle);
		}

		private static Rect GetButtonTooltipRect(Rect bounds, Rect anchor, Vector2 size, bool above)
		{
			float width = Mathf.Min(size.x, bounds.width);
			float height = Mathf.Min(size.y, bounds.height);
			float y = above ? anchor.yMin - height - 4 : anchor.yMax + 4;
			if (!above && y + height > bounds.yMax)
				y = anchor.yMin - height - 4;
			else if (above && y < bounds.yMin)
				y = anchor.yMax + 4;

			return new Rect(Mathf.Clamp(anchor.xMin, bounds.xMin, bounds.xMax - width),
				Mathf.Clamp(y, bounds.yMin, bounds.yMax - height), width, height);
		}
	}
}
