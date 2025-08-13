using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Docs;
using Beamable.Server;
using System.Collections.Concurrent;
using System.CommandLine;
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
		AddOption(new Option<string>("--manifest-id", () => "global", "Defines the name of the manifest that will be used to compare the changes between the manifest and the snapshot. The default value is `global`"), (args, s) => args.ManifestId = s);
	}

	public override async Task<ContentSnapshotListResult> GetResult(ContentSnapshotListCommandArgs args)
	{
		_contentService = args.ContentService;
		
		var localSnapshotPaths = _contentService.GetContentSnapshots(true);
		var sharedSnapshotPaths = _contentService.GetContentSnapshots(false);

		var allContentFiles = await _contentService.GetAllContentFiles(manifestId:args.ManifestId);
		

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
				var contentSnapshotListItems = manifestSnapshot.ContentFiles.Select(contentSnapshot =>
				{
					// Lets check what the current status of the content to be restored
					ContentStatus status = ContentStatus.Invalid;
					int contentManifestReferenceIndex = allContentFiles.ContentFiles.FindIndex(content => content.Id == contentSnapshot.Key);
					// If the content doesn't exist on manifest, it means that it is new
					if (contentManifestReferenceIndex == -1)
					{
						status = ContentStatus.Created;
					}
					else
					{
						var contentManifestRef = allContentFiles.ContentFiles[contentManifestReferenceIndex];
						switch (contentManifestRef.GetStatus())
						{
							case ContentStatus.Modified:
							case ContentStatus.UpToDate:
							case ContentStatus.Created:
								status = contentManifestRef.PropertiesChecksum == contentSnapshot.Value.Checksum
									? ContentStatus.UpToDate
									: ContentStatus.Modified;
								break;
							case ContentStatus.Deleted:
								status = ContentStatus.Created;
								break;
						}
					}
					return new ContentSnapshotListItem() { Name = contentSnapshot.Key, CurrentStatus = (int) status, };
				}).ToList();
				
				// All contents that doesn't exist in the snapshot will be deleted
				contentSnapshotListItems.AddRange(allContentFiles.ContentFiles
					.Where(content => !manifestSnapshot.ContentFiles.ContainsKey(content.Id) && content.GetStatus() != ContentStatus.Deleted).Select(content =>
						new ContentSnapshotListItem() { Name = content.Id, CurrentStatus = (int)ContentStatus.Deleted }));
				
				return new ManifestSnapshotItem()
				{
					Name = Path.GetFileNameWithoutExtension(path),
					Path = path,
					Pid = manifestSnapshot.Pid,
					ManifestId = manifestSnapshot.ManifestId,
					SavedTimestamp = manifestSnapshot.SnapshotTimestamp,
					Contents = contentSnapshotListItems.ToArray()
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
	public string ManifestId;
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
	public string ManifestId;
	public string Pid;
	public long SavedTimestamp;
	public ContentSnapshotListItem[] Contents;
}

[CliContractType, Serializable]
public class ContentSnapshotListItem
{
	public string Name;
	public int CurrentStatus;
	public ContentStatus StatusEnum => (ContentStatus)CurrentStatus;
}
