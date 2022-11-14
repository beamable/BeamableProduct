using Beamable.Common;
using System.Text.Json;

namespace cli.Services;

public class ContentLocalCache
{
	private readonly IAppContext _context;
	private Dictionary<string, ContentDocument> _localAssets;
	private string DirPath => Path.Combine(_context.WorkingDirectory, Constants.CONFIG_FOLDER, "Content");

	public Dictionary<string, ContentDocument> Assets => _localAssets;

	public ContentLocalCache(IAppContext context)
	{
		_context = context;
	}

	public bool HasSameVersion(ContentDocument otherDoc) =>
		Assets.TryGetValue(otherDoc.id, out var localVersion) &&
		localVersion.CalculateChecksum() == otherDoc.CalculateChecksum();

	public async Promise<string> GetContent(string id)
	{
		var path = Path.Combine(DirPath, $"{id}.json");
		var content = await File.ReadAllTextAsync(path);
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
		BeamableLogger.Log($"Writing to: {path}");
		var value = JsonSerializer.Serialize(result.properties.Value, new JsonSerializerOptions { WriteIndented = true } );
		_localAssets[result.id] = result;
		await File.WriteAllTextAsync(path, value);
	}
}
