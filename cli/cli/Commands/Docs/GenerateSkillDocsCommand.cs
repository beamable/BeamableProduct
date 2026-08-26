using Beamable.Common.BeamCli;
using Beamable.Server;
using cli.Docs;
using cli.Services;
using Scriban;
using Scriban.Runtime;
using System.Collections;
using System.CommandLine;
using System.Text;

namespace cli.Commands.Docs;

public class GenerateSkillDocsCommandArgs : CommandArgs
{
	public string templateDir;
}

[CliContractType]
public class GenerateSkillDocsCommandResult
{
	public int RenderedCount;
}

public class GenerateSkillDocsCommand
	: AtomicCommand<GenerateSkillDocsCommandArgs, GenerateSkillDocsCommandResult>, IStandaloneCommand, ISkipManifest
{
	public override bool IsForInternalUse => true;

	public GenerateSkillDocsCommand()
		: base("generate-skill-docs", "Render Scriban skill templates into Markdown")
	{
	}

	public override void Configure()
	{
		AddOption(
			new Option<string>("--template-dir", "Path to the Docs directory containing SkillTemplates/ and Skills/ subdirectories"),
			(args, v) => args.templateDir = v);
	}

	public override async Task<GenerateSkillDocsCommandResult> GetResult(GenerateSkillDocsCommandArgs args)
	{
		var templateDir = Path.Combine(args.templateDir, "SkillTemplates");
		var outputDir = Path.Combine(args.templateDir, "Skills");

		if (!Directory.Exists(templateDir))
		{
			Log.Warning($"Template directory not found: {templateDir}");
			return new GenerateSkillDocsCommandResult { RenderedCount = 0 };
		}

		Directory.CreateDirectory(outputDir);

		var schema = GenerateBeamableTypesSchemaCommand.GenerateLive();
		var commands = BuildCommandsDictionary(args);

		var data = new SkillTemplateData
		{
			FederationTypes = schema.FederationTypes ?? Array.Empty<FederationTypeEntry>(),
			UnrealTypeMappings = schema.UnrealTypeMappings ?? Array.Empty<UnrealTypeMappingEntry>(),
			ServiceCommands = Array.Empty<SkillCommandInfo>(),
			ContentCommands = Array.Empty<SkillCommandInfo>(),
			Commands = commands,
		};

		var templateFiles = Directory.GetFiles(templateDir, "*.md.scriban");
		var renderedCount = 0;

		foreach (var templateFile in templateFiles)
		{
			var templateText = await File.ReadAllTextAsync(templateFile);
			var template = Template.Parse(templateText);

			if (template.HasErrors)
			{
				Log.Error($"Template parse errors in {templateFile}: {string.Join("; ", template.Messages)}");
				continue;
			}

			MemberRenamerDelegate renamer = member =>
			{
				var result = new StringBuilder();
				for (int i = 0; i < member.Name.Length; i++)
				{
					var c = member.Name[i];
					if (char.IsUpper(c) && i > 0)
						result.Append('_');
					result.Append(char.ToLowerInvariant(c));
				}
				return result.ToString();
			};

			var scriptObject = new ScriptObject();
			scriptObject.Import(data, renamer: renamer);

			var context = new TemplateContext();
			context.MemberRenamer = renamer;
			context.PushGlobal(scriptObject);
			var rendered = template.Render(context);

			var outputFileName = Path.GetFileNameWithoutExtension(templateFile);
			var outputPath = Path.Combine(outputDir, outputFileName);
			await File.WriteAllTextAsync(outputPath, rendered);
			Log.Information($"Rendered {outputFileName}");
			renderedCount++;
		}

		Log.Information($"Rendered {renderedCount} skill doc(s) from templates");
		return new GenerateSkillDocsCommandResult { RenderedCount = renderedCount };
	}

	Dictionary<string, SkillCommandDetail> BuildCommandsDictionary(GenerateSkillDocsCommandArgs args)
	{
		var result = new Dictionary<string, SkillCommandDetail>();

		try
		{
			var cliGen = args.DependencyProvider.GetService<CliGenerator>();
			var cliContext = cliGen.GetCliContext();

			foreach (var cmd in cliContext.Commands)
			{
				var key = cmd.executionPath;
				if (key.StartsWith("beam "))
					key = key.Substring(5);
				else if (key == "beam")
					key = "beam";

				var detail = BuildCommandDetail(cmd, key);
				result[key] = detail;

				RegisterAliasedPaths(result, cmd, key, detail);
			}
		}
		catch (Exception ex)
		{
			Log.Warning($"Could not build command tree: {ex.Message}");
		}

		return result;
	}

	SkillCommandDetail BuildCommandDetail(BeamCommandDescriptor cmd, string key)
	{
		return new SkillCommandDetail
		{
			Name = cmd.command.Name,
			Description = cmd.command.Description ?? "",
			ExecutionPath = key,
			Arguments = cmd.command.Arguments
				.Select(a => new SkillCommandArg
				{
					Name = a.Name,
					Description = a.Description ?? "",
					Type = FriendlyTypeName(a.ValueType),
					IsRequired = !a.HasDefaultValue,
				})
				.ToArray(),
			Options = cmd.command.Options
				.Where(o => !o.IsHidden && o.Name != "help")
				.Select(o => new SkillCommandOption
				{
					Name = o.Aliases.FirstOrDefault(a => a.StartsWith("--")) ?? $"--{o.Name}",
					Aliases = o.Aliases.ToArray(),
					Description = o.Description ?? "",
					Type = FriendlyOptionType(o.ValueType),
					IsRequired = o.IsRequired,
				})
				.ToArray(),
		};
	}

	void RegisterAliasedPaths(Dictionary<string, SkillCommandDetail> result, BeamCommandDescriptor cmd, string key, SkillCommandDetail detail)
	{
		var ancestors = new List<(string name, string[] aliases)>();
		var current = cmd;
		while (current != null)
		{
			var aliases = current.command.Aliases
				.Where(a => a != current.command.Name)
				.ToArray();
			ancestors.Insert(0, (current.command.Name, aliases));
			current = current.parent;
		}

		// Skip "beam" root prefix
		if (ancestors.Count > 0 && ancestors[0].name == "beam")
			ancestors.RemoveAt(0);

		var allPaths = new List<string> { key };
		foreach (var (name, aliases) in ancestors)
		{
			if (aliases.Length == 0) continue;
			var expanded = new List<string>();
			foreach (var path in allPaths)
			{
				foreach (var alias in aliases)
				{
					var parts = path.Split(' ');
					for (int i = 0; i < parts.Length; i++)
					{
						if (parts[i] == name)
						{
							var newParts = (string[])parts.Clone();
							newParts[i] = alias;
							expanded.Add(string.Join(' ', newParts));
						}
					}
				}
			}
			allPaths.AddRange(expanded);
		}

		foreach (var aliasedPath in allPaths)
		{
			if (aliasedPath != key)
				result.TryAdd(aliasedPath, detail);
		}
	}

	static string FriendlyTypeName(Type type)
	{
		if (type == null) return "string";
		return type.Name switch
		{
			"String" => "string",
			"Int32" => "int",
			"Int64" => "long",
			"Single" => "float",
			"Double" => "double",
			"Boolean" => "bool",
			_ => type.Name,
		};
	}

	static string FriendlyOptionType(Type type)
	{
		if (type == null) return "string";
		if (type == typeof(bool)) return "flag";
		if (type.IsGenericType && type.IsAssignableTo(typeof(IEnumerable)))
			return $"Set[{FriendlyTypeName(type.GetGenericArguments()[0])}]";
		return FriendlyTypeName(type);
	}
}
