using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
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
			: entry.ChangeKind >= (int)ContentChangeKind.UpToDate && entry.ChangeKind <= (int)ContentChangeKind.IncomingDeletion
				? (ContentChangeKind)entry.ChangeKind : ContentChangeKind.Unknown;

		public static bool IsIncoming(ContentChangeKind kind) => kind is ContentChangeKind.IncomingChange or ContentChangeKind.IncomingAddition or ContentChangeKind.IncomingDeletion;

		public static bool NeedsReconciliation(LocalContentManifestEntry entry) => !entry.IsInConflict &&
			(GetChangeKind(entry) == ContentChangeKind.Unknown || IsIncoming(GetChangeKind(entry)));

		public static bool CanReviewEntry(LocalContentManifestEntry entry) => entry.IsInConflict || NeedsReconciliation(entry);

		public static void ShowReconciliationDialog(CliContentService service, LocalContentManifestEntry entry)
		{
			if (service.CurrentProgress.IsActive) return;
			ContentReconciliationWindow.Open(service, entry);
		}

		private static string AutoSyncDescription(bool localMode) => localMode
			? "Auto-sync is paused while Local Mode is on. Review this item before publishing."
			: "Auto-sync is enabled; reconciliation may still be pending.";

		public static string GetStatusLabel(LocalContentManifestEntry entry, bool isInvalid = false, bool schemaMismatch = false) =>
			isInvalid ? "Validation Error" : schemaMismatch && GetChangeKind(entry) != ContentChangeKind.Conflict ? "Schema mismatch" : GetChangeKind(entry) switch
		{
			ContentChangeKind.UpToDate => "Up to date",
			ContentChangeKind.LocalEdit when entry.StatusEnum == ContentStatus.Deleted => "Deleted",
			ContentChangeKind.LocalEdit => "Local edit",
			ContentChangeKind.IncomingChange => "Incoming change",
			ContentChangeKind.IncomingAddition => "Incoming addition",
			ContentChangeKind.IncomingDeletion => "Incoming deletion",
			ContentChangeKind.Conflict => "Conflict",
			_ => entry.StatusEnum is ContentStatus.Modified or ContentStatus.Created or ContentStatus.Deleted
				? "Differs from publication" : "Status unavailable"
		};

		internal static string GetSchemaWarningDescription(SchemaDifference difference, int affectedCount, bool multiSelection)
		{
			if (affectedCount == 0 || difference == SchemaDifference.None) return string.Empty;
			var message = multiSelection
				? $"Schema mismatch\n{affectedCount} selected item(s) were loaded with a different local C# content schema. Inspect each item for details."
				: "Schema mismatch\nThe content JSON did not match the local C# content type when loaded.";
			if (!multiSelection)
			{
				if ((difference & SchemaDifference.UnknownFieldsInJson) != 0)
					message += "\nUnknownFieldsInJson: The JSON contains fields this Inspector cannot display.";
				if ((difference & SchemaDifference.MissingFieldsInJson) != 0)
					message += "\nMissingFieldsInJson: The local C# type defines fields missing from the JSON.";
			}
			return message + "\nUpdate the C# content type to match the JSON, or intentionally publish the local schema. " +
				"The loader saves fields supported by the local type; publishing can remove unrecognized fields from the realm.";
		}

		public static string GetStatusDescription(LocalContentManifestEntry entry, bool localMode) => GetChangeKind(entry) switch
		{
			ContentChangeKind.UpToDate => "Your local copy matches the last known publication.",
			ContentChangeKind.LocalEdit when entry.StatusEnum == ContentStatus.Deleted =>
				"Your local copy has been deleted. This deletion has not been published.",
			ContentChangeKind.LocalEdit => "Your local copy has unpublished changes.",
			ContentChangeKind.IncomingChange => "Published content changed; your local copy is unchanged. " +
				(localMode ? "Auto-sync is paused while Local Mode is on." : "Auto-sync is enabled; synchronization may still be pending."),
			ContentChangeKind.IncomingAddition => "This item was added in the realm and has not been reconciled locally. " + AutoSyncDescription(localMode),
			ContentChangeKind.IncomingDeletion => "This item was deleted from the realm; your local copy is unchanged. " + AutoSyncDescription(localMode),
			ContentChangeKind.Conflict => "Both your local copy and the published version changed. Resolve the conflict before publishing.",
			_ => entry.StatusEnum is ContentStatus.Modified or ContentStatus.Created or ContentStatus.Deleted
				? "Differs from latest publication. The origin of this difference is unavailable."
				: "Content status is unavailable. The local copy has not been confirmed against the last known publication."
		};

		public static Texture GetStatusIcon(LocalContentManifestEntry entry) => GetChangeKind(entry) switch
		{
			ContentChangeKind.Conflict => EditorGUIUtility.IconContent("console.erroricon.sml").image,
			ContentChangeKind.LocalEdit when entry.StatusEnum == ContentStatus.Deleted => BeamGUI.iconStatusDeleted,
			ContentChangeKind.LocalEdit => BeamGUI.iconStatusModified,
			ContentChangeKind.IncomingChange or ContentChangeKind.IncomingAddition or ContentChangeKind.IncomingDeletion => BeamGUI.iconStatusIncoming,
			_ => EditorGUIUtility.IconContent("console.infoicon.sml").image
		};

		public static string GetSelectionDescription(IReadOnlyList<LocalContentManifestEntry> entries, int historicalCount,
			bool localMode, out bool conflict, int invalidCount = 0, string validationDetails = null)
		{
			conflict = false;
			if (entries.Count == 1 && historicalCount == 0)
			{
				conflict = GetChangeKind(entries[0]) == ContentChangeKind.Conflict;
				if (invalidCount > 0)
				{
					var message = GetStatusLabel(entries[0], true) + "\n" +
						(string.IsNullOrEmpty(validationDetails) ? "Check the highlighted fields in the Inspector." : validationDetails) +
						"\nFix validation errors before publishing.";
					if (conflict || IsIncoming(GetChangeKind(entries[0]))) message += "\n" + GetStatusDescription(entries[0], localMode);
					return message;
				}
				return GetStatusLabel(entries[0]) + "\n" + GetStatusDescription(entries[0], localMode);
			}
			if (historicalCount > 0 && entries.Count == 0)
				return "Historical preview\nShowing published content from the selected History entry, not the current working copy.";
			var counts = new int[7];
			var deletedCount = 0;
			foreach (var entry in entries)
			{
				var kind = GetChangeKind(entry);
				if (kind == ContentChangeKind.LocalEdit && entry.StatusEnum == ContentStatus.Deleted) deletedCount++;
				else counts[(int)kind]++;
			}
			conflict = counts[(int)ContentChangeKind.Conflict] > 0;
			var parts = new List<string>();
			for (var i = 0; i < counts.Length; i++)
				if (counts[i] > 0) parts.Add($"{counts[i]} {GetStatusLabel(new LocalContentManifestEntry { ChangeKind = i })}");
			if (deletedCount > 0) parts.Add($"{deletedCount} Deleted");
			if (historicalCount > 0) parts.Add($"{historicalCount} Historical preview");
			var description = $"{entries.Count + historicalCount} selected: " + string.Join("; ", parts) + ".";
			if (invalidCount > 0) description = $"Validation Error\n{invalidCount} selected item(s) have validation errors. Inspect each item and fix the highlighted fields before publishing.\n" + description;
			if (conflict) description += " Resolve conflicts before publishing.";
			if (counts[(int)ContentChangeKind.IncomingChange] + counts[(int)ContentChangeKind.IncomingAddition] + counts[(int)ContentChangeKind.IncomingDeletion] > 0)
				description += localMode ? " Auto-sync is paused while Local Mode is on." : " Auto-sync is enabled; synchronization may still be pending.";
			return description;
		}

		public GUIContent GetContentStatusBadge(CliContentService contentService, LocalContentManifestEntry entry,
			string renamedFrom = null)
		{
			bool isInvalid = contentService.IsContentInvalid(entry.FullId);
			bool isConflicted = GetChangeKind(entry) == ContentChangeKind.Conflict;
			var schemaDifference = contentService.GetSchemaDifference(entry.FullId);
			var messages = new List<string>();
			if (isInvalid) messages.Add("Validation Error. Inspect this item to view the invalid fields.");
			if (schemaDifference != SchemaDifference.None) messages.Add(GetSchemaWarningDescription(schemaDifference, 1, false));
			if (isInvalid || isConflicted) messages.Add("Publishing is blocked.");
			if (renamedFrom != null) messages.Add($"Renamed from \"{renamedFrom}\".");
			messages.Add(GetStatusDescription(entry, ContentConfiguration.Instance.EnableLocalContentInEditor));
			return new GUIContent(string.Empty, isInvalid || (schemaDifference != SchemaDifference.None && !isConflicted)
				? BeamGUI.iconStatusInvalid : GetStatusIcon(entry), string.Join("\n", messages));
		}
	}
}
