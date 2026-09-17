using System;
using Beamable.Editor.Util;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		/// <summary>
		/// Counts all content, independently of the current search. An item can be both
		/// invalid and conflicted; the overlap keeps the affected-item total distinct.
		/// </summary>
		private PublishIssueSummary GetPublishIssueSummary()
		{
			int invalid = 0, conflicted = 0, both = 0;
			foreach (var entry in _contentService.EntriesCache.Values)
			{
				bool isInvalid = _contentService.IsContentInvalid(entry.FullId);
				bool isConflicted = entry.IsInConflict;
				if (isInvalid) invalid++;
				if (isConflicted) conflicted++;
				if (isInvalid && isConflicted) both++;
			}
			return new PublishIssueSummary(invalid, conflicted, both);
		}

		private void ShowPublishIssuesPopup(Rect anchor)
		{
			ResetButtonTooltip();
			PopupWindow.Show(anchor, new PublishIssuesPopup(GetPublishIssueSummary(), _cli.Permissions.CanPushContent, () =>
			{
				if (this == null) return;
				Focus();
				ShowContentIssues();
				// The filter action runs in the owner's next GUI pass, not in the popup.
				Repaint();
			}));
		}

		private readonly struct PublishIssueSummary
		{
			public readonly int Invalid;
			public readonly int Conflicted;
			public readonly int Both;
			public int Total => Invalid + Conflicted - Both;

			public PublishIssueSummary(int invalid, int conflicted, int both)
			{
				Invalid = invalid;
				Conflicted = conflicted;
				Both = both;
			}
		}

		/// <summary>
		/// Displays a snapshot of publish blockers using the current Unity editor theme.
		/// Dismissing the popup leaves filters untouched; its action opens the live Issues list.
		/// </summary>
		private sealed class PublishIssuesPopup : PopupWindowContent
		{
			private const float Width = 380f;
			private const float Padding = 12f;
			private readonly PublishIssueSummary _issues;
			private readonly bool _hasPublishPermission;
			private readonly Action _showIssues;
			private readonly GUIStyle _bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
			private readonly GUIStyle _headingStyle = new GUIStyle(EditorStyles.boldLabel) { wordWrap = true };

			public PublishIssuesPopup(PublishIssueSummary issues, bool hasPublishPermission, Action showIssues)
			{
				_issues = issues;
				_hasPublishPermission = hasPublishPermission;
				_showIssues = showIssues;
			}

			public override Vector2 GetWindowSize() => new Vector2(Width, LayoutContents(false));

			public override void OnGUI(Rect rect)
			{
				if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
				{
					Event.current.Use();
					editorWindow.Close();
					return;
				}
				LayoutContents(true);
			}

			// Share measurement and drawing so wrapped text and optional notes cannot
			// push the action button outside the popup's calculated height.
			private float LayoutContents(bool draw)
			{
				float contentWidth = Width - Padding * 2f;
				float titleHeight = Mathf.Max(20f, EditorGUIUtility.singleLineHeight);
				float y = Padding;
				if (draw)
				{
					GUI.DrawTexture(new Rect(Padding, y, 20f, 20f), BeamGUI.iconStatusInvalid, ScaleMode.ScaleToFit);
					GUI.Label(new Rect(Padding + 28f, y, contentWidth - 28f, titleHeight), "Publishing blocked", _headingStyle);
				}
				y += titleHeight + 12f;

				Label(_issues.Total > 0
					? $"{_issues.Total} {(_issues.Total == 1 ? "item needs" : "items need")} attention before publishing."
					: "Content status is updating. Review items for the latest details.", _bodyStyle, 12f);
				Label($"Invalid: {_issues.Invalid} {ItemWord(_issues.Invalid)}", _headingStyle, 4f);
				Label("Fix validation errors before publishing.", _bodyStyle, 12f);
				Label($"Conflicts: {_issues.Conflicted} {ItemWord(_issues.Conflicted)}", _headingStyle, 4f);
				Label("Resolve differences between local and remote changes.", _bodyStyle, 12f);

				if (_issues.Both > 0)
					Label($"Included in both counts: {_issues.Both} {ItemWord(_issues.Both)}.", _bodyStyle, 12f);
				if (!_hasPublishPermission)
					Label("You also do not have permission to publish to this realm.", _bodyStyle, 12f);

				if (draw && GUI.Button(new Rect(Padding, y, contentWidth, 28f), "Show Items with Issues"))
				{
					editorWindow.Close();
					_showIssues?.Invoke();
				}
				return y + 28f + Padding;

				void Label(string text, GUIStyle style, float spacing)
				{
					float height = style.CalcHeight(new GUIContent(text), contentWidth);
					if (draw) GUI.Label(new Rect(Padding, y, contentWidth, height), text, style);
					y += height + spacing;
				}
			}

			private static string ItemWord(int count) => count == 1 ? "item" : "items";
		}
	}
}
