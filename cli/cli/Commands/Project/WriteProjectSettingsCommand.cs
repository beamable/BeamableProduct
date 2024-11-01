using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Dependencies;
using Beamable.Common.Semantics;
using cli.Services;
using Serilog;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace cli.Commands.Project;

public class WriteProjectSettingsCommandArgs : CommandArgs
{
	public string beamoId;
	public List<SettingInput> settings = new List<SettingInput>();
	public List<string> settingsToDelete = new List<string>();
	public bool skipBuild;

}

public class SettingInput
{
	public string key, value;
	public bool IsLikelyJson => value.StartsWith('{') || value.StartsWith('[');
}

public class WriteProjectSettingsCommandOutput
{

}

public class WriteProjectSettingsCommand : AtomicCommand<WriteProjectSettingsCommandArgs, WriteProjectSettingsCommandOutput>
{
	public override bool IsForInternalUse => true;

	public WriteProjectSettingsCommand() : base("write-setting", "Write a group of project settings")
	{
		AddAlias("write-settings");
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("beamoId", "The BeamoId to write the settings for"),
			(args, i) => args.beamoId = i);


		var skipBuildOption = new Option<bool>(
			name: "--skip-build",
			description: "If options are modified, the project needs to be re-built so that the " +
						 "embedded resource is available on the next execution of the service. " +
						 "However, this build operation may be skipped when this option is set");
		skipBuildOption.AddAlias("--skip");
		skipBuildOption.AddAlias("-s");
		AddOption(skipBuildOption, (args, i) => args.skipBuild = i);

		var deleteOption = new Option<List<string>>("--delete-key", "The keys of the settings to be deleted. If the key is also set in a --key option, the key will not be deleted");
		deleteOption.AddAlias("-d");
		deleteOption.AllowMultipleArgumentsPerToken = true;
		deleteOption.Arity = ArgumentArity.OneOrMore;

		var keyOption = new Option<List<string>>("--key", "The keys of the settings. The count must match the count of the --value options");
		keyOption.AddAlias("-k");
		keyOption.AllowMultipleArgumentsPerToken = true;
		keyOption.Arity = ArgumentArity.OneOrMore;

		var valueOption = new Option<List<string>>("--value", "The values of the settings. The count must match the count of the --key options");
		valueOption.AddAlias("-v");
		valueOption.AllowMultipleArgumentsPerToken = true;
		valueOption.Arity = ArgumentArity.OneOrMore;

		AddOption(keyOption, (args, i) =>
		{
			// do nothing; the binding happens in the value callback
		});
		AddOption(deleteOption, (args, i) =>
		{
			args.settingsToDelete = i;
		});
		AddOption(valueOption, (args, binder, values) =>
		{

			var keys = binder.ParseResult.GetValueForOption(keyOption);

			if (keys.Count != values.Count)
			{
				throw new CliException("The --key count must match the --value count");
			}

			for (var i = 0; i < values.Count; i++)
			{
				var setting = new SettingInput { key = keys[i], value = values[i] };
				args.settings.Add(setting);
			}
		});

	}

	public override async Task<WriteProjectSettingsCommandOutput> GetResult(WriteProjectSettingsCommandArgs args)
	{
		return await WriteSettings(
			args: args,
			beamoId: args.beamoId,
			settingsToDelete: args.settingsToDelete,
			settings: args.settings,
			skipBuild: args.skipBuild);
	}

	public static async Task<WriteProjectSettingsCommandOutput> WriteSettings(
		CommandArgs args,
		string beamoId,
		List<string> settingsToDelete,
		List<SettingInput> settings,
		bool skipBuild)
	{
		ProjectContextUtil.EvictManifestCache();

		var beamo = args.BeamoLocalSystem;
		var config = args.ConfigService;
		if (!beamo.BeamoManifest.HttpMicroserviceLocalProtocols.TryGetValue(beamoId, out var http))
		{
			throw new CliException("Invalid beamoId");
		}

		var subPath = Path.Combine("temp", "localDev");
		var localDevFolder = Path.GetFullPath(config.GetConfigPath(subPath));
		Directory.CreateDirectory(localDevFolder);

		var imports = FindLocalDevImports(localDevFolder, http.Metadata.msbuildProject);
		if (!TryFindExistingXmlSettings(imports, beamoId, out var docInfo))
		{
			// there is no existing ItemGroup for the given service, so we need to create one...

			if (imports.Count == 0)
			{
				// there were no files at all, so we need to create one
				var newProjectPath = Path.Combine(localDevFolder, "LocalServiceDevelopmentSettings.csproj");
				CreateBlankProjectFile(newProjectPath);
				imports = new List<string> { newProjectPath };
			}

			var file = imports[0];
			if (!TryLoadDocument(file, out var doc, out var projectNode))
			{
				throw new CliException($"csproj file=[{file}] must have a <Project> root tag. ");
			}

			// and now we need to create the item group
			var itemGroup = CreateItemGroup(projectNode, beamoId);
			docInfo = new SettingInfo { csProjPath = file, document = doc, itemGroup = itemGroup };
		}

		// delete all the properties we no longer want
		foreach (var setting in settingsToDelete)
		{
			if (!TryFindExistingSetting(docInfo.itemGroup, setting, out var property))
			{
				// the property doesn't exist anyway; so no work needs to be done
				continue;
			}

			property.Remove();
		}

		// add or set all the properties we do want
		foreach (var setting in settings)
		{
			// try to load the existing setting so we can modify it inline
			if (!TryFindExistingSetting(docInfo.itemGroup, setting.key, out var property))
			{
				// the property didn't exist, so we need to create it
				property = new XElement(CliConstants.PROJECT_BEAMABLE_SETTING);
				property.SetAttributeValue("Include", setting.key);
				AddNewLineAndSpace(docInfo.itemGroup);
				docInfo.itemGroup.Add(property);
				AddNewLineAndSpace(docInfo.itemGroup, -1);
			}

			// set the property, regardless of it was just created or if it already existed
			SetPropertyValue(property, setting);
			docInfo.document.Save(docInfo.csProjPath, SaveOptions.None);
		}

		if (skipBuild)
		{
			return new WriteProjectSettingsCommandOutput();
		}

		{ // build the project, otherwise the newly applied settings won't appear unless the user specifically does a `dotnet run` ahead of time.
		  //  but IDEs (like Rider) won't do that, and won't realize that the csproj file has a meaningful change. 
			var subArgs = args.Create<BuildProjectCommandArgs>();
			Log.Information("Building project to apply settings...");
			await ProjectService.WatchBuild(subArgs, new ServiceName(beamoId), ProjectService.BuildFlags.DisableClientCodeGen, report =>
			{
				if (report.isSuccess)
				{
					Log.Information("Build successful!");
					return;
				}

				Log.Error("Build had warnings or errors!");
				Log.Error(string.Join("\n", report.errors.Select(e => e.formattedMessage)));
			});
		}

		return new WriteProjectSettingsCommandOutput();
	}

	/// <summary>
	/// Load up all the imported project files from a given csproj, and return the filtered list
	/// of projects that exist in the given localDevFolder
	/// </summary>
	/// <param name="localDevFolder"></param>
	/// <param name="project"></param>
	/// <returns></returns>
	public static List<string> FindLocalDevImports(string localDevFolder, Microsoft.Build.Evaluation.Project project)
	{
		var csProjPaths = project.Imports.Where(import =>
			{
				var directory = import.ImportedProject.DirectoryPath;
				var isSameDir = localDevFolder == directory;
				return isSameDir;
			})
			.Select(x => x.ImportedProject.FullPath)
			.ToList();
		return csProjPaths;
	}

	public class SettingInfo
	{
		public string csProjPath;
		public XDocument document;
		public XElement itemGroup;
	}

	/// <summary>
	/// Given a csProj file, load it as an XDocument and find the root Project node.
	/// If the Project node doesn't exist, this function returns false
	/// </summary>
	/// <param name="csProjPath"></param>
	/// <param name="document"></param>
	/// <param name="projectElement"></param>
	/// <returns></returns>
	public static bool TryLoadDocument(string csProjPath, out XDocument document, out XElement projectElement)
	{
		using var stream = File.Open(csProjPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
		using var reader = XmlReader.Create(stream);
		document = XDocument.Load(reader);
		projectElement = default;

		var rootNodes = document.Nodes().ToList();
		projectElement = rootNodes.FirstOrDefault(x => x is XElement element && element.Name == "Project") as XElement;
		if (projectElement == null)
			return false;

		return true;
	}

	/// <summary>
	/// Given a root Project node, add an ItemGroup for a given beamoId.
	/// The ItemGroup will have the correct Label and Condition so that they can be read later.
	/// </summary>
	/// <param name="rootNode"></param>
	/// <param name="beamoId"></param>
	/// <returns></returns>
	public static XElement CreateItemGroup(XElement rootNode, string beamoId)
	{
		var itemGroup = new XElement("ItemGroup");
		itemGroup.SetAttributeValue("Label", CliConstants.BeamableSettingLabel(beamoId));
		itemGroup.SetAttributeValue("Condition", $"'$({CliConstants.PROP_BEAMO_ID})'=='{beamoId}' AND '$({CliConstants.PROP_BEAM_PROJECT_TYPE})'=='service'");

		AddNewLineAndSpace(rootNode);
		rootNode.Add(itemGroup);
		AddNewLine(rootNode);

		return itemGroup;
	}

	static void AddNewLineAndSpace(XElement element, int extraCount = 0)
	{
		element.Add(new XText(Environment.NewLine));
		AddSpace(element, extraCount);
	}

	static void AddNewLine(XElement element)
	{
		element.Add(new XText(Environment.NewLine));
	}

	static void AddSpace(XElement element, int extraCount = 0)
	{
		element.Add(new XText(new string('\t', extraCount + element.AncestorsAndSelf().Count())));
	}


	static void CreateBlankProjectFile(string path)
	{
		var text = @"<Project>
</Project>";
		File.WriteAllText(path, text);
	}

	/// <summary>
	/// Given a set of possible import projects, try to find the first project that has an ItemGroup for the given beamoId.
	/// </summary>
	/// <param name="importPaths"></param>
	/// <param name="beamoId"></param>
	/// <param name="output"></param>
	/// <returns></returns>
	public static bool TryFindExistingXmlSettings(List<string> importPaths, string beamoId, out SettingInfo output)
	{
		output = default;
		foreach (var path in importPaths)
		{
			if (!TryFindExistingXmlSettings(path, beamoId, out var fileOutput))
			{
				continue;
			}

			output = new SettingInfo { csProjPath = path, document = fileOutput.Item1, itemGroup = fileOutput.Item2 };
			return true;
		}

		return false;
	}

	/// <summary>
	/// Try to extract the document and ItemGroup for the required beamoId within a particular csProj file.
	/// The ItemGroup must have a Label that matches the required format for the given BeamoId
	/// </summary>
	/// <param name="csProjPath"></param>
	/// <param name="beamoId"></param>
	/// <param name="output"></param>
	/// <returns></returns>
	public static bool TryFindExistingXmlSettings(string csProjPath, string beamoId, out (XDocument, XElement) output)
	{
		output = default;

		if (!TryLoadDocument(csProjPath, out output.Item1, out var rootNode))
		{
			return false;
		}

		foreach (var node in rootNode.Nodes())
		{
			// the node must be an element
			if (!(node is XElement element)) continue;

			// and it must be an item group
			if (element.Name != "ItemGroup") continue;

			// and it must have a label in the correct format for the given BeamoId
			var hasMatchingLabel = element.Attribute("Label")?.Value.Contains(CliConstants.BeamableSettingLabel(beamoId)) ?? false;
			if (!hasMatchingLabel) continue;

			// if all that is true, then we've found the magic item group!!!
			output.Item2 = element;
			return true;
		}

		return false;
	}

	public static bool TryFindExistingSetting(XElement itemGroup, string key, out XElement property)
	{
		property = default;
		foreach (var node in itemGroup.Nodes())
		{
			// the node must be an XElement
			if (!(node is XElement element)) continue;

			// and the type must be BeamableSetting
			if (element.Name != CliConstants.PROJECT_BEAMABLE_SETTING) continue;

			var hasMatchingKey = element.Attribute("Include")?.Value == key;
			if (!hasMatchingKey) continue;

			property = element;
			return true;
		}

		return false;
	}

	public static void SetPropertyValue(XElement property, SettingInput setting)
	{
		// if there is a "Value" attribute, we should use that...
		if (setting.IsLikelyJson)
		{
			property.SetAttributeValue("ValueTypeHint", "json");
		}
		else
		{
			property.Attribute("ValueTypeHint")?.Remove();
		}

		var valueProp = property.Attribute("Value");
		if (valueProp != null)
		{
			valueProp.Value = setting.value;
		}
		else
		{
			// otherwise, prefer to set the value in the XML
			var subNodes = property.Nodes().ToList();
			var valueNode = subNodes.FirstOrDefault(x => x is XElement valueElement && valueElement.Name == "Value") as XElement;
			if (valueNode == null)
			{
				// add the value
				AddNewLineAndSpace(property);
				property.Add(new XElement("Value") { Value = setting.value });
				AddNewLineAndSpace(property, -1);
			}
			else
			{
				// modify the value
				valueNode.Value = setting.value;
			}
		}
	}

}
