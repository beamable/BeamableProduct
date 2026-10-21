using System;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Content;
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
			int updates = 0, additions = 0, deletions = 0, unknown = 0;
			foreach (var entry in _contentService.EntriesCache.Values)
			{
				bool isInvalid = _contentService.IsContentInvalid(entry.FullId);
				bool isConflicted = entry.IsInConflict;
				if (isInvalid) invalid++;
				if (isConflicted) conflicted++;
				if (isInvalid && isConflicted) both++;
				if (!ContentWindow_TooltipsHelper.NeedsReconciliation(entry)) continue;
				switch (ContentWindow_TooltipsHelper.GetChangeKind(entry))
				{
					case ContentChangeKind.IncomingChange: updates++; break;
					case ContentChangeKind.IncomingAddition: additions++; break;
					case ContentChangeKind.IncomingDeletion: deletions++; break;
					default: unknown++; break;
				}
			}
			return new PublishIssueSummary(invalid, conflicted, both, updates, additions, deletions, unknown);
		}

		private void ShowPublishIssuesPopup(Rect anchor)
		{
			ResetButtonTooltip();
			var summary = GetPublishIssueSummary();
			var scope = $"Realm: {_cli.CurrentRealm.DisplayName}  |  Namespace: {_contentService.GetSelectedManifestIdCliOption() ?? "global"}";
			PopupWindow.Show(anchor, new PublishIssuesPopup(summary, _cli.Permissions.CanPushContent, scope,
				ContentConfiguration.Instance.EnableLocalContentInEditor, () =>
			{
				if (this == null) return;
				Focus();
				ShowContentIssues(summary.Total == 0);
				// The filter action runs in the owner's next GUI pass, not in the popup.
				Repaint();
			}));
		}

		private readonly struct PublishIssueSummary
		{
			public readonly int Invalid;
			public readonly int Conflicted;
			public readonly int Both;
			public readonly int Updates, Additions, Deletions, Unknown;
			public int Pending => Updates + Additions + Deletions + Unknown;
			public int Total => Invalid + Conflicted - Both;

			public PublishIssueSummary(int invalid, int conflicted, int both, int updates, int additions, int deletions, int unknown)
			{
				Invalid = invalid;
				Conflicted = conflicted;
				Both = both;
				Updates = updates; Additions = additions; Deletions = deletions; Unknown = unknown;
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
			private readonly string _scope;
			private readonly bool _localMode;
			private readonly GUIStyle _bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
			private readonly GUIStyle _headingStyle = new GUIStyle(EditorStyles.boldLabel) { wordWrap = true };

			public PublishIssuesPopup(PublishIssueSummary issues, bool hasPublishPermission, string scope, bool localMode, Action showIssues)
			{
				_issues = issues;
				_scope = scope; _localMode = localMode;
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

				bool incoming = _issues.Total == 0;
				if (incoming)
				{
					Label(_scope, _bodyStyle, 12f);
					Label($"Incoming updates: {_issues.Updates}  |  Additions: {_issues.Additions}  |  Deletions: {_issues.Deletions}", _headingStyle, 8f);
					if (_issues.Unknown > 0) Label($"Review needed: {_issues.Unknown} items with unverified change history.", _bodyStyle, 8f);
					Label("Reconcile these items before publishing. Your local edits are preserved. " +
						(_localMode ? "Local Mode remains on." : "Auto-sync remains enabled."), _bodyStyle, 12f);
				}
				else
				{
					Label($"{_issues.Total} {(_issues.Total == 1 ? "item needs" : "items need")} attention before publishing.", _bodyStyle, 12f);
					Label($"Invalid: {_issues.Invalid} {ItemWord(_issues.Invalid)}", _headingStyle, 4f);
					Label("Fix validation errors before publishing.", _bodyStyle, 12f);
					Label($"Conflicts: {_issues.Conflicted} {ItemWord(_issues.Conflicted)}", _headingStyle, 4f);
					Label("Resolve differences between local and remote changes.", _bodyStyle, 12f);
					if (_issues.Both > 0) Label($"Included in both counts: {_issues.Both} {ItemWord(_issues.Both)}.", _bodyStyle, 12f);
					if (_issues.Pending > 0) Label($"After resolving these issues, {_issues.Pending} incoming or unverified items also need review.", _bodyStyle, 12f);
				}
				if (!_hasPublishPermission)
					Label("You also do not have permission to publish to this realm.", _bodyStyle, 12f);

				if (draw && GUI.Button(new Rect(Padding, y, contentWidth, 28f), incoming ? "Review Incoming Items" : "Show Items with Issues"))
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
