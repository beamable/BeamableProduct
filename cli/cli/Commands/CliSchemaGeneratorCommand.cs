using Beamable.Common.Util;
using Beamable.Server;
using cli.Services;
using cli.Services.Sandbox;
using Beamable.Server.Common;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System.CommandLine;

namespace cli;

public class CliSchemaGeneratorCommandArgs : CommandArgs
{
	[CanBeNull] public string OutputPath;
	public bool Pretty;
}

/// <summary>
/// Emits a structured JSON description of the CLI command surface — the runtime
/// counterpart to <c>generate-interface --engine=ts</c>. Portal asks the sandbox
/// for this at pair-time and diffs it against its own bundled schema to figure
/// out which commands are usable across version-skew.
/// </summary>
public class CliSchemaGeneratorCommand : AppCommand<CliSchemaGeneratorCommandArgs>, IStandaloneCommand, ISkipManifest
{
	public override bool IsForInternalUse => true;

	public CliSchemaGeneratorCommand() : base("generate-schema",
		"Emits a JSON schema describing the CLI command surface for runtime use") { }

	public override void Configure()
	{
		AddOption(new Option<string>("--output", () => null,
				"When null or empty, the schema is written to stdout. Otherwise the path to write the JSON to"),
			(args, val) => args.OutputPath = val);

		AddOption(new Option<bool>("--pretty", () => false,
				"When true, the JSON is indented"),
			(args, val) => args.Pretty = val);
	}

	public override Task Handle(CliSchemaGeneratorCommandArgs args)
	{
		var ctx = args.DependencyProvider.GetService<CliGenerator>().GetCliContext();
		var schema = BuildSchema(ctx);
		var json = JsonConvert.SerializeObject(schema,
			args.Pretty ? Formatting.Indented : Formatting.None);

		if (string.IsNullOrEmpty(args.OutputPath))
		{
			Console.WriteLine(json);
		}
		else
		{
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args.OutputPath)) ?? ".");
			File.WriteAllText(args.OutputPath, json);
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// Build a <see cref="CliSchema"/> from a walked command tree. Exposed publicly
	/// so the sandbox runtime can serve the same schema over its <c>GetSchema</c>
	/// route — Portal then diffs its bundled schema against this at pair-time.
	/// </summary>
	public static CliSchema BuildSchema(CliGeneratorContext ctx)
	{
		var commands = ctx.Commands
			.Where(c => c.executionPath != "beam")
			.Where(c => !c.IsInternalSubtree)
			.OrderBy(c => c.executionPath, StringComparer.Ordinal)
			.Select(BuildCommandSpec)
			.ToList();

		// Walk reachable result-stream types + sandbox wire types for the type registry.
		var allTypes = new HashSet<Type>();
		foreach (var c in ctx.Commands)
		{
			foreach (var s in c.resultStreams)
			{
				CollectReachable(s.runtimeType, allTypes);
			}
		}
		foreach (var t in SandboxWireRoots)
		{
			CollectReachable(t, allTypes);
		}

		var types = allTypes
			.OrderBy(t => t.Name, StringComparer.Ordinal)
			.Select(BuildTypeSpec)
			.Where(spec => spec != null)
			.ToList();

		return new CliSchema
		{
			cliVersion = SafeGetVersion(),
			commands = commands,
			types = types,
		};
	}

	private static readonly Type[] SandboxWireRoots = new[]
	{
		typeof(SandboxBatchDto),
		typeof(SandboxEventDto),
		typeof(SandboxPairResponse),
		typeof(SandboxInfoResponse),
		typeof(SandboxInvokeResponse),
		typeof(SandboxGetOutputResponse),
		typeof(SandboxListInvocationsResponse),
		typeof(SandboxListDirResponse),
		typeof(SandboxStatResponse),
		typeof(SandboxReadFileResponse),
		typeof(SandboxWriteFileResponse),
		typeof(SandboxWatchPathsResponse),
		typeof(SandboxOkResponse),
		typeof(SandboxCancelResponse),
		typeof(SandboxShutdownResponse),
	};

	private static CliCommandSpec BuildCommandSpec(BeamCommandDescriptor cmd)
	{
		return new CliCommandSpec
		{
			path = cmd.executionPath,
			name = cmd.GetName(),
			description = cmd.command.Description,
			args = cmd.command.Arguments
				.OrderBy(a => a.Name, StringComparer.Ordinal)
				.Select(a => new CliArgSpec
				{
					name = a.Name,
					description = a.Description,
					typeName = DescribeType(a.ValueType),
					required = !a.HasDefaultValue,
				})
				.ToList(),
			options = cmd.command.Options
				.OrderBy(o => o.Name, StringComparer.Ordinal)
				.Select(o => new CliOptionSpec
				{
					name = o.Name,
					description = o.Description,
					typeName = DescribeType(o.ValueType),
					required = o.IsRequired,
					allowMultiple = o.AllowMultipleArgumentsPerToken,
				})
				.ToList(),
			results = cmd.resultStreams
				.OrderBy(s => s.channel, StringComparer.Ordinal)
				.Select(s => new CliResultSpec
				{
					channel = s.channel,
					typeName = s.runtimeType.Name,
				})
				.ToList(),
		};
	}

	private static CliTypeSpec BuildTypeSpec(Type t)
	{
		if (t.IsPrimitive || t == typeof(string) || t == typeof(Guid)) return null;
		if (t.IsEnum) return null;
		if (t.IsArray) return null;
		if (t.IsGenericType) return null;

		List<CliFieldSpec> fields;
		try
		{
			fields = UnityJsonContractResolver.GetSerializedFields(t)
				.OrderBy(f => f.Name, StringComparer.Ordinal)
				.Select(f =>
				{
					var fieldType = f.FieldType;
					var optional = false;
					if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
					{
						optional = true;
						fieldType = fieldType.GetGenericArguments()[0];
					}
					return new CliFieldSpec
					{
						name = f.Name,
						typeName = DescribeType(fieldType),
						optional = optional,
					};
				}).ToList();
		}
		catch
		{
			return null;
		}

		return new CliTypeSpec { name = t.Name, fields = fields };
	}

	private static void CollectReachable(Type root, HashSet<Type> output)
	{
		var queue = new Queue<Type>();
		queue.Enqueue(root);
		while (queue.Count > 0)
		{
			var t = queue.Dequeue();
			if (t == null) continue;

			if (t.IsArray) { queue.Enqueue(t.GetElementType()); continue; }
			if (t.IsGenericType)
			{
				var gen = t.GetGenericTypeDefinition();
				if (gen == typeof(Nullable<>) || gen == typeof(List<>) || gen == typeof(IEnumerable<>))
				{
					queue.Enqueue(t.GetGenericArguments()[0]);
					continue;
				}
				if (gen == typeof(Dictionary<,>))
				{
					queue.Enqueue(t.GetGenericArguments()[1]);
					continue;
				}
			}

			if (t.IsPrimitive || t == typeof(string) || t == typeof(Guid) || t == typeof(DateTime)
			    || t == typeof(DateTimeOffset) || t == typeof(decimal) || t == typeof(object)) continue;
			if (t.IsEnum) continue;
			if (!output.Add(t)) continue;

			try
			{
				foreach (var f in UnityJsonContractResolver.GetSerializedFields(t))
				{
					queue.Enqueue(f.FieldType);
				}
			}
			catch
			{
				// ignore reflection failures
			}
		}
	}

	private static string DescribeType(Type t)
	{
		if (t == null) return "any";
		if (t == typeof(bool)) return "boolean";
		if (t == typeof(string) || t == typeof(char) || t == typeof(Guid)) return "string";
		if (t == typeof(DateTime) || t == typeof(DateTimeOffset)) return "string";
		if (t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort)
		    || t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong)
		    || t == typeof(float) || t == typeof(double) || t == typeof(decimal)) return "number";
		if (t.IsArray) return DescribeType(t.GetElementType()) + "[]";
		if (t.IsGenericType)
		{
			var gen = t.GetGenericTypeDefinition();
			var args = t.GetGenericArguments();
			if (gen == typeof(Nullable<>)) return DescribeType(args[0]);
			if (gen == typeof(List<>) || gen == typeof(IEnumerable<>) || gen == typeof(IList<>))
				return DescribeType(args[0]) + "[]";
			if (gen == typeof(Dictionary<,>))
				return $"Record<string, {DescribeType(args[1])}>";
		}
		if (t.IsEnum) return "string";
		return t.Name;
	}

	private static string SafeGetVersion()
	{
		try { return BeamAssemblyVersionUtil.GetVersion<CliSchemaGeneratorCommand>(); }
		catch { return "0.0.0"; }
	}
}

// Serialized shapes — public fields with camelCase names so the emitted JSON
// matches the TypeScript types Portal will consume directly.

public class CliSchema
{
	public string cliVersion;
	public List<CliCommandSpec> commands;
	public List<CliTypeSpec> types;
}

public class CliCommandSpec
{
	public string path;
	public string name;
	public string description;
	public List<CliArgSpec> args;
	public List<CliOptionSpec> options;
	public List<CliResultSpec> results;
}

public class CliArgSpec
{
	public string name;
	public string description;
	public string typeName;
	public bool required;
}

public class CliOptionSpec
{
	public string name;
	public string description;
	public string typeName;
	public bool required;
	public bool allowMultiple;
}

public class CliResultSpec
{
	public string channel;
	public string typeName;
}

public class CliTypeSpec
{
	public string name;
	public List<CliFieldSpec> fields;
}

public class CliFieldSpec
{
	public string name;
	public string typeName;
	public bool optional;
}
