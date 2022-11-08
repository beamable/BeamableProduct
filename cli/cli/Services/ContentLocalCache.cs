using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Serialization.SmallerJSON;

namespace cli.Services;

public class ContentLocalCache
{
	private readonly IAppContext _context;
	private readonly CliRequester _requester;
	private Dictionary<string, string> _localAssetsVersions;
	private string DirPath => Path.Combine(_context.WorkingDirectory, Constants.CONFIG_FOLDER, "Content", $"{_context.Cid}_{_context.Pid}");

	public Dictionary<string, string> AssetsVersions => _localAssetsVersions;

	public ContentLocalCache(IAppContext context, CliRequester requester)
	{
		_context = context;
		_requester = requester;
	}

	public bool HasSameVersion(ClientContentInfo contentInfo) =>
		AssetsVersions.TryGetValue(contentInfo.contentId, out var localVersion) &&
		localVersion == contentInfo.version;

	public async Promise<string> GetContent(string id)
	{
		var path = Path.Combine(DirPath, $"{id}.json");
		var content = await File.ReadAllTextAsync(path);
		return content;
	}
	
	
	public void Init()
	{
		if (_localAssetsVersions != null)
			return;
		
		if (!Directory.Exists(DirPath))
		{
			Directory.CreateDirectory(DirPath);
		}
		_localAssetsVersions = new Dictionary<string, string>();

		foreach (var path in Directory.EnumerateFiles(DirPath, "*json"))
		{
			var fileContent = File.ReadAllText(path);
			var arrayDict = (ArrayDict)Json.Deserialize(fileContent);
			_localAssetsVersions.Add(arrayDict["id"] as string ?? throw new InvalidOperationException(), arrayDict["version"] as string);
			
			BeamableLogger.Log(Path.GetFileName(path));
		}
	}

	public async Promise UpdateData(ClientManifest manifest)
	{
		Init();
		foreach (var contentInfo in manifest.entries)
		{
			if (_localAssetsVersions.TryGetValue(contentInfo.contentId, out var localVersion) &&
			    localVersion == contentInfo.version)
				continue;
			try
			{
				var result = await _requester.CustomRequest(Method.GET, contentInfo.uri, parser: s => s);
				BeamableLogger.Log($"Writing to: {Path.Combine(DirPath, $"{contentInfo.contentId}.json")}");
				await File.WriteAllTextAsync(Path.Combine(DirPath, $"{contentInfo.contentId}.json"), result);
			}
			catch (Exception e)
			{
				BeamableLogger.LogException(e);
			}
		}
	}

	public async Task UpdateContent(ClientContentInfo contentInfo, string result)
	{
		var path = Path.Combine(DirPath, $"{contentInfo.contentId}.json");
		BeamableLogger.Log($"Writing to: {path}");
		await File.WriteAllTextAsync(path, result);
	}
}
