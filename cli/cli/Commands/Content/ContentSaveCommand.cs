using Beamable.Common.Content;
using Beamable.Server;
using System.CommandLine;

namespace cli.Content;

public class ContentSaveCommand : AppCommand<ContentSaveCommandArgs>, IEmptyResult, ISkipManifest
{
	private static readonly Option<string[]> CONTENT_ID_OPTION = new("--content-ids", Array.Empty<string>, "An array of existing content ids");

	private static readonly Option<string[]> CONTENT_PROPERTIES_ID_OPTION =
		new("--content-properties", "An array, parallel to the --content-ids, that contain the escaped properties json for each content");

	private static readonly ConfigurableOptionFlag FORCE_OPTION =
		new("force", "When this is set, this will ignore your local state and save the properties directly to disk");

	private ContentService _contentService;

	public ContentSaveCommand() : base("save",
		"Saves a serialized content properties JSON-blob into a manifest (expects the blob to be in Beamable's Serialization Format). " +
		"This command is not meant for manual usage. It is meant for engine integrations and CI/CD content enforcing use-cases." +
		"Editing of content is to be made either via engine integrations OR via a JSON text-editor")
	{
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIds = s);
		AddOption(CONTENT_ID_OPTION, (args, s) => args.ContentIds = s);
		AddOption(CONTENT_PROPERTIES_ID_OPTION, (args, s) => args.ContentProperties = s);
		AddOption(FORCE_OPTION, (args, b) => args.Force = b);
	}

	public override async Task Handle(ContentSaveCommandArgs args)
	{
		var tokenSource = new CancellationTokenSource();
		_contentService = args.ContentService;

		Log.Verbose("Validating Content Save Command.");

		// Ensure Manifest Ids is correct
		if (args.ManifestIds.Length == 0) args.ManifestIds = new[] { "global" };
		else if (args.ManifestIds.Length > 1)
			throw new CliException("Must pass a single Manifest Id per-command invocation.", 6, true);

		// Invalid args
		if (args.ContentIds.Length != args.ContentProperties.Length)
			throw new CliException("Content Ids and Content Properties have different lengths. Please provide parallel arrays for these options.", 2, true);

		Log.Verbose("Validated Content Save Command.");

		var manifestId = args.ManifestIds[0];

		Log.Verbose("Getting Local Cache For Manifest. MANIFEST_ID={0}", manifestId);
		// Loads the known local content in relation to the remote one (OR just the local one if Force is true)
		var localCache = await _contentService.GetAllContentFiles(null,
			manifestId,
			ContentFilterType.ExactIds,
			args.ContentIds,
			args.Force);

		Log.Verbose("Saving Content.\nMANIFEST_ID={0}.\nIDS_PROPS={1}",
			manifestId,
			string.Join("\n", args.ContentIds.Zip(args.ContentProperties).Select((id, props) => $"({id}, {props})")));

		// Save the new contentId/contentProperties pairs
		await _contentService.BulkSaveLocalContent(localCache, args.ContentIds, args.ContentProperties, tokenSource.Token);

		Log.Verbose("Saved Content.\nMANIFEST_ID={0}.\nIDS_PROPS={1}",
			manifestId,
			string.Join("\n", args.ContentIds.Zip(args.ContentProperties).Select((id, props) => $"({id}, {props})")));
	}
}

public class ContentSaveCommandArgs : ContentCommandArgs
{
	public string[] ContentIds;
	public string[] ContentProperties;
	public bool Force;
}
