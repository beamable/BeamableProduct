using Beamable.Common.BeamCli.Contracts;
using Beamable.Content;
using Beamable.Editor.ContentService;
using Beamable.Editor.Util;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.ContentWindow
{
	public class ContentWindow_TooltipsHelper
	{
		public static ContentChangeKind GetChangeKind(LocalContentManifestEntry entry) => entry.IsInConflict
			? ContentChangeKind.Conflict
			: entry.ChangeKind >= (int)ContentChangeKind.UpToDate && entry.ChangeKind <= (int)ContentChangeKind.Conflict
				? (ContentChangeKind)entry.ChangeKind : ContentChangeKind.Unknown;

		public static string GetStatusLabel(LocalContentManifestEntry entry) => GetChangeKind(entry) switch
		{
			ContentChangeKind.UpToDate => "Up to date",
			ContentChangeKind.LocalEdit => "Local edit",
			ContentChangeKind.IncomingChange => "Incoming change",
			ContentChangeKind.Conflict => "Conflict",
			_ => entry.StatusEnum is ContentStatus.Modified or ContentStatus.Created or ContentStatus.Deleted
				? "Differs from publication" : "Status unavailable"
		};

		public static string GetStatusDescription(LocalContentManifestEntry entry, bool localMode) => GetChangeKind(entry) switch
		{
			ContentChangeKind.UpToDate => "Your local copy matches the last known publication.",
			ContentChangeKind.LocalEdit => "Your local copy has unpublished changes.",
			ContentChangeKind.IncomingChange => "Published content changed; your local copy is unchanged. " +
				(localMode ? "Auto-sync is paused while Local Mode is on." : "Auto-sync is enabled; synchronization may still be pending."),
			ContentChangeKind.Conflict => "Both your local copy and the published version changed. Resolve the conflict before publishing.",
			_ => entry.StatusEnum is ContentStatus.Modified or ContentStatus.Created or ContentStatus.Deleted
				? "Differs from latest publication. The origin of this difference is unavailable."
				: "Content status is unavailable. The local copy has not been confirmed against the last known publication."
		};

		public static Texture GetStatusIcon(LocalContentManifestEntry entry) => GetChangeKind(entry) switch
		{
			ContentChangeKind.Conflict => EditorGUIUtility.IconContent("console.erroricon.sml").image,
			ContentChangeKind.LocalEdit => BeamGUI.iconStatusModified,
			ContentChangeKind.IncomingChange => BeamGUI.iconStatusIncoming,
			_ => EditorGUIUtility.IconContent("console.infoicon.sml").image
		};

		public static string GetSelectionDescription(IReadOnlyList<LocalContentManifestEntry> entries, int historicalCount,
			bool localMode, out bool conflict)
		{
			conflict = false;
			if (entries.Count == 1 && historicalCount == 0)
			{
				conflict = GetChangeKind(entries[0]) == ContentChangeKind.Conflict;
				return GetStatusLabel(entries[0]) + "\n" + GetStatusDescription(entries[0], localMode);
			}
			if (historicalCount > 0 && entries.Count == 0)
				return "Historical preview\nShowing published content from the selected History entry, not the current working copy.";
			var counts = new int[5];
			foreach (var entry in entries) counts[(int)GetChangeKind(entry)]++;
			conflict = counts[(int)ContentChangeKind.Conflict] > 0;
			var parts = new List<string>();
			for (var i = 0; i < counts.Length; i++)
				if (counts[i] > 0) parts.Add($"{counts[i]} {GetStatusLabel(new LocalContentManifestEntry { ChangeKind = i })}");
			if (historicalCount > 0) parts.Add($"{historicalCount} Historical preview");
			var description = $"{entries.Count + historicalCount} selected: " + string.Join("; ", parts) + ".";
			if (conflict) description += " Resolve conflicts before publishing.";
			if (counts[(int)ContentChangeKind.IncomingChange] > 0)
				description += localMode ? " Auto-sync is paused while Local Mode is on." : " Auto-sync is enabled; synchronization may still be pending.";
			return description;
		}

		public GUIContent GetContentStatusBadge(CliContentService contentService, LocalContentManifestEntry entry,
			string renamedFrom = null)
		{
			bool isInvalid = contentService.IsContentInvalid(entry.FullId);
			bool isConflicted = GetChangeKind(entry) == ContentChangeKind.Conflict;
			var messages = new List<string>();
			if (isInvalid) messages.Add("Validation failed. Select Validate to view the errors.");
			if (isInvalid || isConflicted) messages.Add("Publishing is blocked.");
			if (renamedFrom != null) messages.Add($"Renamed from \"{renamedFrom}\".");
			messages.Add(GetStatusDescription(entry, ContentConfiguration.Instance.EnableLocalContentInEditor));
			return new GUIContent(string.Empty, isInvalid ? BeamGUI.iconStatusInvalid : GetStatusIcon(entry), string.Join("\n", messages));
		}
	}
}
