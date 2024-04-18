using Beamable.Common.BeamCli;
using cli.Services.Content;

namespace cli.Content;

public class ContentLocalManifestCommand : AtomicCommand<ContentLocalManifestCommandArgs, LocalContentState>
{
	private ContentService _contentService;

	public ContentLocalManifestCommand() : base("local-manifest", "Gets the current local manifest")
	{
	}

	public override bool IsForInternalUse => true;

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestFilter = s);
	}

	public override async Task<LocalContentState> GetResult(ContentLocalManifestCommandArgs args)
	{
		_contentService = args.ContentService;

		// Prepare the filters (makes sure all the given manifests exist.
		var manifestsToGet = await _contentService.PrepareManifestFilter(args.ManifestFilter);
		
		// If we have no manifests... auto-create a "global" one. 
		if (manifestsToGet.Length == 0) 
			await _contentService.PublishContentAndManifest("global");

		// Get the local content state for all the requested manifests
		var allLocalStates = await _contentService.GetLocalContentForManifests(manifestsToGet);

		// Builds the local content state from a list of local states
		return _contentService.BuildLocalContentState(manifestsToGet, allLocalStates);
	}
}

public class ContentLocalManifestCommandArgs : ContentCommandArgs
{
	public string[] ManifestFilter;
}

[CliContractType]
public struct LocalContentManifestEntry
{
	public string FullId;
	public string TypeName;
	public string Name;

	/// <summary>
	/// Tied to <see cref="ContentStatus"/>.
	/// </summary>
	public int CurrentStatus;

	public string Hash;

	public string[] Tags;
}

[CliContractType]
public class LocalContentManifest
{
	public string ManifestId;
	public LocalContentManifestEntry[] Entries;
}

[CliContractType]
public class LocalContentState
{
	public LocalContentManifest[] Manifests;
}
