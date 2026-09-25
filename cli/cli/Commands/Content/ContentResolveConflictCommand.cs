using Beamable.Common.Content;
using System.CommandLine;

namespace cli.Content;

public class ContentResolveConflictCommand : AtomicCommand<ContentResolveConflictCommandArgs, ContentResolveConflictResult>, ISkipManifest
{
	private ContentService _contentService;

	public ContentResolveConflictCommand() : base("resolve", "Resolve conflicts between local changes and realm-based changes")
	{
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIdsToReset = s);
		AddOption(ContentCommand.FILTER_TYPE_OPTION, (args, s) => args.FilterType = s);
		AddOption(ContentCommand.FILTER_OPTION, (args, s) => args.Filter = string.IsNullOrEmpty(s) ? Array.Empty<string>() : s.Split(','));
		AddOption(new Option<string>("--use", "Whether to use the \'local\' or \'realm\' version of the conflicted content.\n" +
		                                      "This applies to ALL matching elements of the filter that are conflicted.\n" +
		                                      "Value must be \"local\" or \"realm\""), (args, s) => args.Use = s);
	}

	public override async Task<ContentResolveConflictResult> GetResult(ContentResolveConflictCommandArgs args)
	{
		_contentService = args.ContentService;

		// Verify that the 
		if (args.Use is not ("local" or "realm"))
			throw new CliException("\'--use\' must be \"local\" or \"realm\".", 2, true);

		// Fetches the state relative to the latest manifest
		var latestManifest = await _contentService.GetManifest(replaceLatest: true);
		var fileTasks = new List<Task<LocalContentFiles>>();
		foreach (var manifestId in args.ManifestIdsToReset)
		{
			var task = _contentService.GetAllContentFiles(latestManifest, manifestId, args.FilterType, args.Filter);
			fileTasks.Add(task);
		}

		// Update the local files based on the use flag.
		var files = await Task.WhenAll(fileTasks);
		var saveTasks = new List<Task>();
		var allConflicts = new List<string>();
		foreach (LocalContentFiles lf in files)
		{
			var contentFolder = _contentService.EnsureContentPathForRealmExists(out _, args.AppContext.Pid, lf.ManifestId);
			var conflicts = lf.ContentFiles.Where(c => c.IsInConflict).ToArray();
			allConflicts.AddRange(conflicts.Select(c => c.Id));
			if (args.Use == "local")
			{
				saveTasks.AddRange(conflicts.Select(c => Task.Run(async () =>
				{
					c.FetchedFromManifestUid = latestManifest.uid.GetOrElse("");
					await _contentService.SaveContentFile(contentFolder, c);
				})));
			}

			if (args.Use == "realm" && conflicts.Length > 0)
			{
				// Taking the realm version is a sync of just the conflicted ids, so let the sync do the download and bookkeeping.
				await _contentService.SyncLocalContent(latestManifest, lf.ManifestId, ContentFilterType.ExactIds, conflicts.Select(c => c.Id).ToArray(),
					syncCreated: false, syncModified: false, forceSyncConflicts: true, syncDeleted: false);
			}
			// After resolving conflicts we can also update the manifest references for all contents that are not updated
			saveTasks.Add(_contentService.AdvanceManifestReferences(contentFolder, lf.ContentFiles, latestManifest.uid.GetOrElse("")));
		}

		await Task.WhenAll(saveTasks);
		return new()
		{
			ResolvedContentIds = allConflicts.ToArray(),
		};
	}
}

public class ContentResolveConflictCommandArgs : CommandArgs
{
	public string[] ManifestIdsToReset;

	public ContentFilterType FilterType;
	public string[] Filter;

	public string Use;
}

public class ContentResolveConflictResult
{
	public string[] ResolvedContentIds;
}
