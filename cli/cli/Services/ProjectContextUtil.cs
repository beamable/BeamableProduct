using Beamable.Common.BeamCli.Contracts;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;

namespace cli.Services;

public static class ProjectContextUtil
{
	public static async Task<CsharpProjectMetadata[]> FindCsharpProjects(string dotnetPath, string rootFolder)
	{
		var paths = Directory.GetFiles(rootFolder, "*.csproj", SearchOption.AllDirectories);
		var projects = new CsharpProjectMetadata[paths.Length];

		var propertyTasks = new List<Task<CsharpProjectBuildData>>();
		for (var i = 0 ; i < paths.Length; i ++)
		{
			var path = paths[i];
			Log.Verbose($"Found csproj=[{path}]");
			projects[i] = new CsharpProjectMetadata
			{
				relativePath = Path.GetRelativePath(rootFolder, path),
				absolutePath = path,
				fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path),
				// properties = props
			};
			
			var task = GetCsharpProperties(dotnetPath, path, properties: new string[]
			{
				CliConstants.PROP_BEAMO_ID,
				CliConstants.PROP_BEAM_ENABLED,
				CliConstants.PROP_BEAM_PROJECT_TYPE,
			});
			propertyTasks.Add(task);
		}

		var propertyResults = await Task.WhenAll(propertyTasks);
		for (var i = 0; i < paths.Length; i++)
		{
			projects[i].properties = propertyResults[i].Properties;
			projects[i].projectReferences = propertyResults[i].Items.ProjectReference;
		}

		return projects;
	}

	public static async Task<CsharpProjectBuildData> GetCsharpProperties(string dotnetPath, string csharpPath, params string[] properties)
	{
		var propertyList = new HashSet<string>(properties);
		propertyList.Add("TargetFramework");
		propertyList.Add("OutDir");
		var propertyStringQuery = string.Join(",", propertyList);
		var argString = $"dotnet msbuild -getItem:ProjectReference -getProperty:{propertyStringQuery} {csharpPath}";
		var (result, buffer) = await CliExtensions.RunWithOutput(dotnetPath, argString);
		var json = buffer.ToString();
		Log.Verbose("got json: " + json);
		var instance = JsonConvert.DeserializeObject<CsharpProjectBuildData>(json);
		instance.Sanitize();
		return instance;
	}

	public static BeamoServiceDefinition ConvertProjectToServiceDefinition(CsharpProjectMetadata project, Dictionary<string, CsharpProjectMetadata> absPathToProject)
	{
		if (project.properties.ProjectType != "service")
		{
			throw new CliException(
				$"Assert failed; Trying to convert a project into a service project, but the project=[{project.absolutePath}] is of type=[{project.properties.ProjectType}]. This should have been prefiltered in the CLI. ");
		}

		string ConvertBeamoId(CsharpProjectMetadata metadata) => string.IsNullOrEmpty(metadata.properties.BeamId)
			? metadata.fileNameWithoutExtension
			: metadata.properties.BeamId;
		
		var definition = new BeamoServiceDefinition();

		// the beamId will default to the csproj file name
		definition.BeamoId = ConvertBeamoId(project);

		// a service defaults to enabled, unless specifically disabled. 
		if (!bool.TryParse(project.properties.Enabled, out definition.ShouldBeEnabledOnRemote))
		{
			definition.ShouldBeEnabledOnRemote = true;
		}

		// the project directory is just "where is the csproj" 
		definition.ProjectDirectory = Path.GetDirectoryName(project.relativePath);

		definition.Protocol = BeamoProtocolType.HttpMicroservice;
		definition.Language = BeamoServiceDefinition.ProjectLanguage.CSharpDotnet;
		
		foreach (var referencedProject in project.projectReferences)
		{
			if (!absPathToProject.TryGetValue(referencedProject.FullPath, out var knownProject))
			{
				Log.Warning($"Project=[{project.relativePath}] references project=[${referencedProject.FullPath}] but that project was not detected in the beamable folder context. ");
				continue;
			}

			var referenceType = knownProject.properties.ProjectType;
			switch (referenceType)
			{
				case "storage":
					definition.StorageDependencyBeamIds.Add(ConvertBeamoId(knownProject));
					break;
				default:
					definition.GeneralDependencyProjectPaths.Add(knownProject.relativePath);
					break;
			}
		}
		
		
		return definition;
	}
	
	public static BeamoServiceDefinition ConvertProjectToStorageDefinition(CsharpProjectMetadata project, Dictionary<string, CsharpProjectMetadata> absPathToProject)
	{
		if (project.properties.ProjectType != "storage")
		{
			throw new CliException(
				$"Assert failed; Trying to convert a project into a storage project, but the project=[{project.absolutePath}] is of type=[{project.properties.ProjectType}]. This should have been prefiltered in the CLI. ");
		}

		string ConvertBeamoId(CsharpProjectMetadata metadata) => string.IsNullOrEmpty(metadata.properties.BeamId)
			? metadata.fileNameWithoutExtension
			: metadata.properties.BeamId;
		
		var definition = new BeamoServiceDefinition();

		// the beamId will default to the csproj file name
		definition.BeamoId = ConvertBeamoId(project);

		// a service defaults to enabled, unless specifically disabled. 
		if (!bool.TryParse(project.properties.Enabled, out definition.ShouldBeEnabledOnRemote))
		{
			definition.ShouldBeEnabledOnRemote = true;
		}

		// the project directory is just "where is the csproj" 
		definition.ProjectDirectory = Path.GetDirectoryName(project.relativePath);

		definition.Protocol = BeamoProtocolType.EmbeddedMongoDb;
		definition.Language = BeamoServiceDefinition.ProjectLanguage.CSharpDotnet;
		
		foreach (var referencedProject in project.projectReferences)
		{
			if (!absPathToProject.TryGetValue(referencedProject.FullPath, out var knownProject))
			{
				Log.Warning($"Project=[{project.relativePath}] references project=[${referencedProject.FullPath}] but that project was not detected in the beamable folder context. ");
				continue;
			}

			var referenceType = knownProject.properties.ProjectType;
			switch (referenceType)
			{
				case "storage":
					definition.StorageDependencyBeamIds.Add(ConvertBeamoId(knownProject));
					break;
				default:
					definition.GeneralDependencyProjectPaths.Add(knownProject.relativePath);
					break;
			}
		}
		
		
		return definition;
	}



	public class MsBuildProjectProperties
	{
		[JsonProperty(CliConstants.PROP_BEAMO_ID)]
		public string BeamId;

		[JsonProperty(CliConstants.PROP_BEAM_ENABLED)]
		public string Enabled;

		[JsonProperty(CliConstants.PROP_BEAM_PROJECT_TYPE)]
		public string ProjectType;
	}
	
	public class MsBuildProjectReference
	{
		[JsonProperty("Identity")]
		public string RelativePath;

		[JsonProperty("FullPath")]
		public string FullPath;

		[JsonProperty(CliConstants.ATTR_BEAM_REF_TYPE)]
		public string BeamRefType;
	}

	public class MsBuildProjectItems
	{
		[JsonProperty("ProjectReference")]
		public List<MsBuildProjectReference> ProjectReference = new List<MsBuildProjectReference>();
	}


	
	[Serializable]
	public class CsharpProjectBuildData
	{
		[JsonProperty("Properties")] // the name is taken from msbuild's output.
		public MsBuildProjectProperties Properties = new MsBuildProjectProperties();

		[JsonProperty("Items")] // the name is taken from msbuild's output.
		public MsBuildProjectItems Items = new MsBuildProjectItems();

		public void Sanitize()
		{
			// TODO: clean up values of properties (like trim, lowercase, etc)
		}
	}

	[Serializable]
	[DebuggerDisplay("{fileNameWithoutExtension} : {relativePath}")]
	public class CsharpProjectMetadata
	{
		public string relativePath;
		public string absolutePath;
		public string fileNameWithoutExtension;

		public MsBuildProjectProperties properties;
		public List<MsBuildProjectReference> projectReferences;
	}
	
}
