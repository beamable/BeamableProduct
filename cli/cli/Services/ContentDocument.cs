using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace cli.Services;

public class ContentDocument
{
	public string id { get; set; }
	public string version { get; set; }
	public JsonElement? properties { get; set; }

	public string CalculateChecksum()
	{
		using var md5 = MD5.Create();
		string json = properties?.ToString();
		if (json != null)
		{
			var bytes = Encoding.ASCII.GetBytes(json);
			var hash = md5.ComputeHash(bytes);
			var checksum = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
			return checksum;
		}

		return string.Empty;
	}

	public static ContentDocument AtPath(string path)
	{
		var fileContent = File.ReadAllText(path);
		var id = Path.GetFileName(path).Replace(".json", string.Empty);
		var properties = JsonSerializer.Deserialize<JsonElement>(fileContent);
		var content = new ContentDocument { id = id, version = string.Empty, properties = properties };
		return content;
	}
}
