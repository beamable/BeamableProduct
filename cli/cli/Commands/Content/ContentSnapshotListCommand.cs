using Beamable.Common.BeamCli;
using Beamable.Server;
using System.Collections.Concurrent;
using System.Text.Json;

namespace cli.Content;

public class ContentSnapshotListCommand : AtomicCommand<ContentSnapshotListCommandArgs, ContentSnapshotListResult>, ISkipManifest
{
	private ContentService _contentService;

	public ContentSnapshotListCommand() : base("snapshot-list", "Find and list all shared (.beamable/content-snapshots) and local (.beamable/temp/content-snapshots) snapshots")
	{
	}

	public override void Configure()
	{
	}

	public override async Task<ContentSnapshotListResult> GetResult(ContentSnapshotListCommandArgs args)
	{
		_contentService = args.ContentService;
		
		var localSnapshotPaths = _contentService.GetContentSnapshots(true);
		var sharedSnapshotPaths = _contentService.GetContentSnapshots(false);

		var parseLocalSnapshots = localSnapshotPaths.Select(async path => await PaseManifestSnapshot(path));
		var parseSharedSnapshots = sharedSnapshotPaths.Select(async path => await PaseManifestSnapshot(path));

		try
		{
			var localSnapshots = await Task.WhenAll(parseLocalSnapshots);
			var sharedSnapshots = await Task.WhenAll(parseSharedSnapshots);
			
			return new ContentSnapshotListResult() { LocalSnapshots = localSnapshots, SharedSnapshots = sharedSnapshots, };
		}
		catch (Exception e)
		{
			Log.Information(e.Message);
			throw;
		}
		

		async Task<ManifestSnapshotItem> PaseManifestSnapshot(string path)
		{
			try
			{
				string manifestContent = await File.ReadAllTextAsync(path);
				var manifestSnapshot = JsonSerializer.Deserialize<ManifestSnapshot>(manifestContent, ContentService.GetContentFileSerializationOptions());
				var contentSnapshotListItems = manifestSnapshot.ContentFiles.Select(contentSnapshot => new ContentSnapshotListItem()
				{
					Name = contentSnapshot.Key, Checksum = contentSnapshot.Value.Checksum,
				}).ToArray();
				return new ManifestSnapshotItem()
				{
					Name = Path.GetFileNameWithoutExtension(path),
					Path = path,
					Contents = contentSnapshotListItems
				};
			}
			catch (Exception e)
			{
				Log.Information(e.Message);
				throw;
			}
		}
	}
	
}

public class ContentSnapshotListCommandArgs : ContentCommandArgs
{
}

[CliContractType, Serializable]
public class ContentSnapshotListResult
{
	public ManifestSnapshotItem[] SharedSnapshots;
	public ManifestSnapshotItem[] LocalSnapshots;
}

[CliContractType, Serializable]
public class ManifestSnapshotItem
{
	public string Name;
	public string Path;
	public ContentSnapshotListItem[] Contents;
	public ContentSnapshotType Type;
}

[CliContractType, Serializable]
public class ContentSnapshotListItem
{
	public string Name;
	public string Checksum;
}
