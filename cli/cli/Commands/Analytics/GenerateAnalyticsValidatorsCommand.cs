using System.CommandLine;
using System.Text.Json;
using Beamable.Server;
using cli.Services.Analytics;
using cli.Utils;
using JetBrains.Annotations;

namespace cli.Commands.Analytics;

public class GenerateAnalyticsValidatorsCommandArgs : CommandArgs
{
	[CanBeNull] public string OutputPath;
	public string ApiPrefix;
}

public class GenerateAnalyticsValidatorsCommandResult
{
	public int GeneratedCount;
	public List<string> GeneratedFiles = new();
}

public class GenerateAnalyticsValidatorsCommand
	: AtomicCommand<GenerateAnalyticsValidatorsCommandArgs, GenerateAnalyticsValidatorsCommandResult>,
	  ISkipManifest
{
	private static readonly JsonSerializerOptions SchemaParseOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
	};

	public GenerateAnalyticsValidatorsCommand()
		: base("generate-validators",
			   "Generate Unreal C++ FBeamAnalyticsEvent subtypes from analytics event JSON Schema definitions")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--output", () => null,
				"Directory where generated .h files will be written. When omitted, files are written into linked Unreal projects (under the core module's Public/Analytics/AutoGen folder); if no Unreal projects are linked, file contents are logged to stdout"),
			(args, val) => args.OutputPath = val);

		AddOption(new Option<string>("--prefix", () => string.Empty,
				"Optional prefix prepended to every generated struct name (e.g. \"Game\" -> FGame<Name>Event)"),
			(args, val) => args.ApiPrefix = val ?? string.Empty);
	}

	public override async Task<GenerateAnalyticsValidatorsCommandResult> GetResult(
		GenerateAnalyticsValidatorsCommandArgs args)
	{
		var api = (AnalyticEventsApi)args.Provider.GetService(typeof(AnalyticEventsApi));

		var views = await api.GetAll();
		Log.Information($"Fetched {views.Count} analytics event schema(s) from the realm");

		var events = new List<AnalyticsEventSchema>(views.Count);
		using var http = new HttpClient();
		foreach (var view in views)
		{
			if (view.Archived || !view.Enabled)
			{
				Log.Verbose($"Skipping event '{view.Name}' — archived or disabled");
				continue;
			}

			var uri = view.Schema?.Uri;
			if (string.IsNullOrEmpty(uri))
			{
				Log.Warning($"Skipping event '{view.Name}' — no schema URI returned");
				continue;
			}

			var body = await http.GetStringAsync(uri);
			var schema = JsonSerializer.Deserialize<JsonSchemaBody>(body, SchemaParseOptions)
			             ?? new JsonSchemaBody();

			// The realm-level Category field is authoritative — it's what users edit in the portal.
			// Override the schema body's x-beamCategory if the view carries one.
			if (!string.IsNullOrEmpty(view.Category))
				schema.BeamCategory = view.Category;

			events.Add(new AnalyticsEventSchema
			{
				Name = view.Name,
				Description = view.Description ?? string.Empty,
				Enabled = view.Enabled,
				Schema = schema,
			});
		}

		var generator = new AnalyticsValidatorGenerator();
		var files = generator.Generate(events, args.ApiPrefix ?? string.Empty);

		var result = new GenerateAnalyticsValidatorsCommandResult
		{
			GeneratedCount = files.Count,
		};

		var hasOutputPath = !string.IsNullOrEmpty(args.OutputPath);
		var linkedUnrealProjects = args.ProjectService.GetLinkedUnrealProjects();

		// No Unreal target and no explicit output — fall back to logging file contents to stdout.
		if (linkedUnrealProjects.Count == 0 && !hasOutputPath)
		{
			foreach (var f in files)
			{
				result.GeneratedFiles.Add(f.FileName);
				Log.Information($"=== {f.FileName} ===");
				Log.Information(f.Content);
			}
			return result;
		}

		// No Unreal target but explicit output — write a flat directory of headers.
		if (linkedUnrealProjects.Count == 0)
		{
			Directory.CreateDirectory(args.OutputPath);
			foreach (var f in files)
			{
				var dest = Path.Combine(args.OutputPath, f.FileName);
				var destDir = Path.GetDirectoryName(dest);
				if (!string.IsNullOrEmpty(destDir)) Directory.CreateDirectory(destDir);
				await File.WriteAllTextAsync(dest, f.Content);
				result.GeneratedFiles.Add(dest);
				Log.Information($"Wrote {dest}");
			}
			return result;
		}

		// Write into each linked UE project under the core module's public headers in an
		// Analytics/AutoGen subfolder. Strictly additive: existing headers (including those for
		// events that have since been archived/deleted on the realm) are left in place. Files
		// that are regenerated this run get overwritten in-place; everything else is untouched.
		const string analyticsSubFolder = "Analytics";
		const string autoGenFolder = "AutoGen";
		foreach (var unrealProjectData in linkedUnrealProjects)
		{
			var outputDir = hasOutputPath
				? args.OutputPath
				: Path.Combine(args.ConfigService.BeamableWorkspace, unrealProjectData.SourceFilesPath);

			var allFilesToCreate = files
				.Select(f => Path.Combine(outputDir, unrealProjectData.MsCoreHeaderPath, analyticsSubFolder, autoGenFolder, f.FileName))
				.ToList();

			var needsProjectFilesRebuild = !allFilesToCreate.All(File.Exists);
			Log.Verbose($"decided to 're-generate project files' for project {unrealProjectData.CoreProjectName} path=[{unrealProjectData.Path}] will-regen=[{needsProjectFilesRebuild}]");

			var writeFiles = new List<Task>();
			for (var i = 0; i < allFilesToCreate.Count; i++)
			{
				var fileIdx = i;
				var filePath = allFilesToCreate[fileIdx];
				var path = Path.GetDirectoryName(filePath);
				if (path == null) throw new CliException($"Parent path for file {filePath} is null. If you're a customer seeing this, report a bug.");

				bool successfulCreate;
				do
				{
					try
					{
						Directory.CreateDirectory(path);
						successfulCreate = true;
					}
					catch
					{
						successfulCreate = false;
					}
				} while (!successfulCreate);

				writeFiles.Add(Task.Run(() =>
				{
					bool successfulWrite;
					do
					{
						try
						{
							File.WriteAllText(filePath, files[fileIdx].Content);
							successfulWrite = true;
						}
						catch
						{
							successfulWrite = false;
						}
					} while (!successfulWrite);

					Log.Verbose($"writing analytics validator to {unrealProjectData.CoreProjectName} dir=[{filePath}]");
				}));
				result.GeneratedFiles.Add(filePath);
			}

			await Task.WhenAll(writeFiles);

			if (needsProjectFilesRebuild)
			{
				Log.Verbose($"regenerating project files for UE {unrealProjectData.CoreProjectName} path=[{unrealProjectData.Path}]");
				MachineHelper.RunUnrealGenerateProjectFiles(Path.Combine(args.ConfigService.BeamableWorkspace, unrealProjectData.Path));
				Log.Verbose($"completed regeneration of project files for UE {unrealProjectData.CoreProjectName} path=[{unrealProjectData.Path}]");
			}
		}

		return result;
	}
}
