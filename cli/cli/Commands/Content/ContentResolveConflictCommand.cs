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
		AddOption(new Option<bool>("--preview", "Preview one selected item's local and realm values without reconciling."), (args, s) => args.Preview = s);
		AddOption(new Option<string>("--expected-local-revision", "Reject reconciliation if the reviewed local file or realm values changed."), (args, s) => args.ExpectedLocalRevision = s);
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

		if (args.Preview || args.ExpectedLocalRevision != null)
		{
			if (args.ManifestIdsToReset?.Length != 1 || args.FilterType != ContentFilterType.ExactIds ||
				args.Filter?.Length != 1 || string.IsNullOrWhiteSpace(args.Filter[0]))
				throw new CliException("Preview and reviewed reconciliation require one manifest and one exact content ID.", 2, true);
			if (args.Preview)
			{
				return new ContentResolveConflictResult
				{
					ResolvedContentIds = Array.Empty<string>(),
					Preview = await _contentService.PreviewReconciliation(args.ManifestIdsToReset[0], args.Filter[0],
						args.ExpectedManifestUid, args.Lifecycle.CancellationToken)
				};
			}
			if (args.Use is not ("local" or "realm"))
				throw new CliException("'--use' must be \"local\" or \"realm\".", 2, true);
			return new ContentResolveConflictResult
			{
				ResolvedContentIds = await _contentService.ReconcileReviewedContent(args.ManifestIdsToReset[0], args.Filter[0],
					args.Use == "local", args.ExpectedManifestUid, args.ExpectedLocalRevision, args.Lifecycle.CancellationToken)
			};
		}

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
	public bool Preview;
	public string ExpectedLocalRevision;
	public string[] ManifestIdsToReset;

	public ContentFilterType FilterType;
	public string[] Filter;

	public string Use;
	public string ExpectedManifestUid;
}

public class ContentResolveConflictResult
{
	public string[] ResolvedContentIds;
	public ContentReconciliationPreview Preview;
}
