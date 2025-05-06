using cli.Services.Content;
using System.CommandLine;
using System.Text.Json;

namespace cli.Content;

public class ContentBulkEditCommand : AppCommand<ContentBulkEditCommandArgs>, IEmptyResult
{
	private static readonly Option<string[]> CONTENT_ID_OPTION = new("--content-ids", Array.Empty<string>, "An array of existing content ids");

	private static readonly Option<string[]> CONTENT_PROPERTIES_ID_OPTION =
		new("--content-properties", Array.Empty<string>, "An array, parallel to the --content-ids, that contain the escaped properties json for each content");

	private ContentService _contentService;

	public ContentBulkEditCommand() : base("bulk-edit", "Saves a serialized content properties JSON-blob into a manifest (expects the blob to be in Beamable's Serialization Format)")
	{
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIds = s);
		AddOption(CONTENT_ID_OPTION, (args, s) => args.ContentIds = s);
		AddOption(CONTENT_PROPERTIES_ID_OPTION, (args, s) => args.ContentProperties = s);
	}

	public override async Task Handle(ContentBulkEditCommandArgs args)
	{
		_contentService = args.ContentService;

		// Ensure Manifest Ids is correct
		if (args.ManifestIds.Length == 0)
			args.ManifestIds = new[] { "global" };
		else if (args.ManifestIds.Length > 1)
			throw new CliException("Must pass a single Manifest Id per-command invocation.", 6, true);

		var manifestId = args.ManifestIds[0];

		// Tries to load the local content --- this command does not work unless the manifest is already known.
		var localCache = await _contentService.GetAllContentFiles(null, manifestId, true);

		// Invalid args
		if (args.ContentIds.Length != args.ContentProperties.Length)
			throw new CliException("Content Ids and Content Properties have different lengths. Please provide parallel arrays for these options.", 2, true);

		var updatedDocuments = new List<ContentFile>(args.ContentIds.Length);
		var notFoundIds = new List<string>(args.ContentIds.Length);
		for (int i = 0; i < args.ContentIds.Length; i++)
		{
			// This command does not create new content objects; so, we check if the given id already exists before proceeding.
			string id = args.ContentIds[i];
			if (!localCache.PerIdContentFiles.TryGetValue(id, out ContentFile file))
			{
				notFoundIds.Add(id);
				continue;
			}

			var newProperties = args.ContentProperties[i];

			// Make sure this is serialized properly and sorted
			var newPropsJson = JsonSerializer.Deserialize<JsonElement>(newProperties);
			newPropsJson = JsonSerializer.SerializeToElement(newPropsJson, ContentService.GetContentFileSerializationOptions());

			// Set this as the new JsonElement for the content file.
			file.Properties = newPropsJson;

			// Store the content file
			updatedDocuments.Add(file);
		}

		// If we the given ids are non-existent, we notify the user.
		if (notFoundIds.Count > 0)
		{
			var err = $"Content Id provided was not found in the given manifest. IDs={string.Join(",", notFoundIds)}";
			throw new CliException(err, 3, true);
		}

		// Save the actual updated content
		try
		{
			var contentFolder = _contentService.EnsureContentPathForRealmExists(args.AppContext.Pid, manifestId);
			var updateTasks = updatedDocuments.Select(d => _contentService.SaveContentFile(contentFolder, d));
			await Task.WhenAll(updateTasks);
		}
		catch (Exception e)
		{
			var err = $"Failed to save Content. Errors:{e.Message}";
			throw new CliException(err, 5, true);
		}
	}
}

public class ContentBulkEditCommandArgs : ContentCommandArgs
{
	public string[] ContentIds;
	public string[] ContentProperties;
}
