using Beamable.Common.Content;
using Beamable.Common.BeamCli.Contracts;
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
		AddOption(new Option<string>("--expected-manifest-uid", "Reject reconciliation if the reviewed publication is no longer latest."), (args, s) => args.ExpectedManifestUid = s);
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

		var resolved = new List<string>();
		foreach (var manifestId in args.ManifestIdsToReset)
		{
			var latest = await _contentService.GetManifest(manifestId, replaceLatest: true);
			var files = await _contentService.GetAllContentFiles(latest, manifestId, args.FilterType, args.Filter);
			// Without a selection preserve the old command's conflict-only scope.
			var ids = files.ContentFiles.Where(c => c.IsInConflict ||
				(args.Filter?.Length > 0 && c.ChangeKind is ContentChangeKind.Unknown or ContentChangeKind.IncomingChange or ContentChangeKind.IncomingAddition or ContentChangeKind.IncomingDeletion))
				.Select(c => c.Id).ToArray();
			if (ids.Length == 0) continue;
			resolved.AddRange(await _contentService.ReconcileContent(manifestId, ids, args.Use == "local", args.Lifecycle.CancellationToken,
				args.ExpectedManifestUid ?? latest.uid.GetOrElse("")));
		}
		return new() { ResolvedContentIds = resolved.ToArray() };
	}
}

public class ContentResolveConflictCommandArgs : CommandArgs
{
	public string[] ManifestIdsToReset;

	public ContentFilterType FilterType;
	public string[] Filter;

	public string Use;
	public string ExpectedManifestUid;
}

public class ContentResolveConflictResult
{
	public string[] ResolvedContentIds;
}
