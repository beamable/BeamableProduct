using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Evaluation.Context;
using Microsoft.Build.FileSystem;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;

namespace cli.Services;

public static class ProjectContextUtil
{
	public static async IAsyncEnumerable<CsharpProjectMetadata> FindCsharpProjects(string dotnetPath, string rootFolder)
	{
		var paths = Directory.GetFiles(rootFolder, "*.csproj", SearchOption.AllDirectories);
		foreach (var path in paths)
		{
			Log.Verbose($"Found csproj=[{path}]");
			var props = await GetCsharpProperties(dotnetPath, path, properties: new string[]
			{
				"BeamProject"
			});
			
			yield return new CsharpProjectMetadata
			{
				relativePath = Path.GetRelativePath(rootFolder, path),
				absolutePath = path,
				fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path),
				properties = props
			};
		}
	}

	public static async Task<CsharpProjectPropertiesCollection> GetCsharpProperties(string dotnetPath, string csharpPath, params string[] properties)
	{
		var propertyList = new HashSet<string>(properties);
		propertyList.Add("TargetFramework");
		propertyList.Add("OutDir");
		var propertyStringQuery = string.Join(",", propertyList);
		var (result, buffer) = await CliExtensions.RunWithOutput(dotnetPath, $"dotnet msbuild -getProperty:{propertyStringQuery} {csharpPath}");
		var json = buffer.ToString();
		var instance = JsonConvert.DeserializeObject<CsharpProjectPropertiesCollection>(json);

		return instance;
	}

	[Serializable]
	public class CsharpProjectPropertiesCollection
	{
		[JsonProperty("Properties")] // the name is taken from msbuild's output.
		public Dictionary<string, string> properties = new Dictionary<string, string>();

		public string TargetFramework => properties["TargetFramework"];
	}

	[Serializable]
	[DebuggerDisplay("{fileNameWithoutExtension} : {relativePath}")]
	public class CsharpProjectMetadata
	{
		public string relativePath;
		public string absolutePath;
		public string fileNameWithoutExtension;

		public CsharpProjectPropertiesCollection properties;
	}
	
}
