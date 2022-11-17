using Beamable.Common;
using Beamable.Common.Content;
using System.Text.Json;

namespace cli.Services;

public class ContentLocalCache
{
	private readonly IAppContext _context;
	private Dictionary<string, ContentDocument> _localAssets;
	private string DirPath => Path.Combine(_context.WorkingDirectory, Constants.CONFIG_FOLDER, "Content");

	public Dictionary<string, ContentDocument> Assets => _localAssets;

	public Dictionary<string, ClientManifest> Manifests => _manifests;

	private readonly Dictionary<string, ClientManifest> _manifests = new ();

	public ContentLocalCache(IAppContext context)
	{
		_context = context;
	}

	public void UpdateManifest(string manifestId, ClientManifest manifest)
	{
		_manifests[manifestId] = manifest;
	}

	public bool HasSameVersion(ClientContentInfo contentInfo)
	{
		if (Assets.TryGetValue(contentInfo.contentId, out var localVersion))
		{
			var local = localVersion.CalculateChecksum();
			return local == contentInfo.version;
		}

		return false;
	}

	public async Promise<ContentDocument?> GetContent(string id)
	{
		var path = Path.Combine(DirPath, $"{id}.json");
		var content = ContentDocument.AtPath(path);
		return content;
	}
	
	
	public void Init()
	{
		if (_localAssets != null)
			return;
		
		if (!Directory.Exists(DirPath))
		{
			Directory.CreateDirectory(DirPath);
		}
		_localAssets = new Dictionary<string, ContentDocument>();

		foreach (var path in Directory.EnumerateFiles(DirPath, "*json"))
		{
			var content = ContentDocument.AtPath(path);
			_localAssets.Add(content.id, content);
		}
	}

	public async Task UpdateContent(ContentDocument result)
	{
		var path = Path.Combine(DirPath, $"{result.id}.json");
		var value = JsonSerializer.Serialize(result.properties.Value, new JsonSerializerOptions { WriteIndented = true } );
		_localAssets[result.id] = result;
		await File.WriteAllTextAsync(path, value);
	}
}
