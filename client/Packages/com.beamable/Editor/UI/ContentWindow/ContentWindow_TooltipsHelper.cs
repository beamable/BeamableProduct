using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.ContentService;
using Beamable.Editor.Util;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Editor.UI.ContentWindow
{
	public class ContentWindow_TooltipsHelper
	{
		private readonly string CreatedTooltip = "Created locally. Publishing will add this item to the current realm.";
		private readonly string ModifiedTooltip = "Modified locally. Publishing will update this item in the current realm.";
		private readonly string DeletedTooltip = "Deleted locally. Publishing will remove this item from the current realm.";
		private readonly string UpToDateTooltip = "Matches the last known realm version.";
		private readonly string InvalidTooltip = "Validation failed. Select Validate to view the errors";
		private readonly string ConflictedTooltip = "Conflicting local and remote changes. Resolve the conflict before publishing";
		private readonly string PublishingBlockedTooltip = "Publishing is blocked.";

		public GUIContent GetContentStatusBadge(CliContentService contentService, LocalContentManifestEntry entry,
			string renamedFrom = null, Texture defaultIcon = null)
		{
			bool isInvalid = contentService.IsContentInvalid(entry.FullId);
			bool isConflicted = entry.IsInConflict;
			bool isRenamed = renamedFrom != null;

			Texture icon;
			if (isInvalid)
				icon = BeamGUI.iconStatusInvalid;
			else if (isConflicted)
				icon = BeamGUI.iconStatusConflicted;
			else if (isRenamed)
				icon = BeamGUI.iconStatusModified;
			else
				icon = defaultIcon;

			string changeDescription = isRenamed
				? $"Renamed from \"{renamedFrom}\"."
				: entry.StatusEnum switch
				{
					ContentStatus.Created => CreatedTooltip,
					ContentStatus.Modified => ModifiedTooltip,
					ContentStatus.Deleted => DeletedTooltip,
					ContentStatus.UpToDate => UpToDateTooltip,
					_ => "Content status unavailable."
				};

			var messages = new List<string>();

			if (isInvalid)
				messages.Add(InvalidTooltip);

			if (isConflicted)
				messages.Add(ConflictedTooltip);

			if (isInvalid || isConflicted)
				messages.Add(PublishingBlockedTooltip);

			messages.Add(changeDescription);

			return new GUIContent(
				string.Empty,
				icon,
				string.Join("\n", messages));
		}
	}
}
