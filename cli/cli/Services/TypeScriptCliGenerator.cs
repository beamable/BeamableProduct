using Beamable.Common.Util;
using Beamable.Server;
using Beamable.Server.Common;
using cli.Services.Sandbox;
using cli.Services.Web.CodeGen;
using System.CommandLine;

namespace cli.Services;

/// <summary>
/// Emits TypeScript bindings (commands, result DTOs, and sandbox wire types)
/// from the live CLI command tree + a hardcoded list of sandbox event/wire types.
///
/// This is the seam that lets Portal (and other web consumers) speak to the
/// CLI/sandbox with end-to-end types. Output lands as a small set of TS files
/// the consumer can import: <c>commands.ts</c>, <c>sandbox.ts</c>,
/// <c>version.ts</c>, <c>index.ts</c>.
/// </summary>
public class TypeScriptCliGenerator : ICliGenerator
{
	/// <summary>
	/// Sandbox wire types that don't appear in any command's result stream but
	/// must still be emitted as TypeScript for Portal to consume. Kept as an
	/// explicit list so it's obvious when adding a new sandbox route/event.
	/// </summary>
	private static readonly Type[] SandboxWireTypes = new[]
	{
		typeof(SandboxBatchDto),
		typeof(SandboxEventDto),
		typeof(SandboxPairResponse),
		typeof(SandboxInfoResponse),
		typeof(SandboxConnectionEventDto),
		typeof(SandboxShutdownResponse),
		typeof(SandboxInvokeResponse),
		typeof(SandboxListInvocationsResponse),
		typeof(SandboxInvocationDto),
		typeof(SandboxGetOutputResponse),
		typeof(SandboxOutputLineDto),
		typeof(SandboxCancelResponse),
		typeof(SandboxOkResponse),
		typeof(SandboxListDirResponse),
		typeof(SandboxDirEntryDto),
		typeof(SandboxStatResponse),
		typeof(SandboxReadFileResponse),
		typeof(SandboxWriteFileResponse),
		typeof(SandboxWatchPathsResponse),
		typeof(SandboxGetSchemaResponse),
		// Schema types — surface them so Portal can type the GetSchema response
		// and call diffSchemas() with both sides strongly typed.
		typeof(CliSchema),
		typeof(CliCommandSpec),
		typeof(CliArgSpec),
		typeof(CliOptionSpec),
		typeof(CliResultSpec),
		typeof(CliTypeSpec),
		typeof(CliFieldSpec),
	};

	public List<GeneratedFileDescriptor> Generate(CliGeneratorContext context)
	{
		var files = new List<GeneratedFileDescriptor>();

		// Walk command tree → arg interfaces + collect result types
		var (commandsFile, commandResultTypes) = GenerateCommandsFile(context);
		files.Add(commandsFile);

		files.Add(GenerateSandboxFile(commandResultTypes));
		files.Add(GenerateVersionFile());
		files.Add(GenerateIndexFile());

		return files;
	}

	private static (GeneratedFileDescriptor file, HashSet<Type> emittedResultTypes) GenerateCommandsFile(
		CliGeneratorContext context)
	{
		var tsFile = new TsFile("commands.ts");
		var emittedTypeNames = new HashSet<string>();

		// Stable, deterministic command order so generated files don't churn between runs.
		var commands = context.Commands
			.Where(c => c.executionPath != "beam")
			.Where(c => !c.IsInternalSubtree)
			.OrderBy(c => c.executionPath, StringComparer.Ordinal)
			.ToList();

		// Per-command Args interfaces
		foreach (var cmd in commands)
		{
			var argsInterface = BuildArgsInterface(cmd);
			if (argsInterface == null) continue;
			argsInterface.AddModifier(TsModifier.Export);
			tsFile.AddDeclaration(argsInterface);
		}

		// Per-command Result interfaces (transitively walk result type graphs)
		var allResultTypes = new HashSet<Type>();
		foreach (var cmd in commands)
		{
			foreach (var stream in cmd.resultStreams)
			{
				CollectReachableTypes(stream.runtimeType, allResultTypes);
			}
		}

		var orderedResultTypes = allResultTypes
			.OrderBy(t => GetTsTypeName(t), StringComparer.Ordinal)
			.ToList();
		foreach (var t in orderedResultTypes)
		{
			var iface = BuildInterfaceForType(t);
			if (iface == null) continue;
			if (!emittedTypeNames.Add(iface.Name)) continue;
			iface.AddModifier(TsModifier.Export);
			tsFile.AddDeclaration(iface);
		}

		// Manifest constant — the union of command paths as a string literal type,
		// plus a runtime map from path → channels. Consumers can use this for
		// autocomplete and lightweight runtime validation.
		var manifestPaths = commands.Select(c => c.executionPath).ToList();
		var pathLiteralUnion = manifestPaths.Count == 0
			? TsType.Never
			: TsType.Union(manifestPaths.Select(p => TsType.Of($"'{p}'")).ToArray());

		var pathTypeAlias = new TsTypeAlias("BeamCommandPath").SetType(pathLiteralUnion);
		pathTypeAlias.AddModifier(TsModifier.Export);
		tsFile.AddDeclaration(pathTypeAlias);

		return (new GeneratedFileDescriptor { FileName = "commands.ts", Content = tsFile.Render() }, allResultTypes);
	}

	private static GeneratedFileDescriptor GenerateSandboxFile(HashSet<Type> alreadyEmittedTypes)
	{
		var tsFile = new TsFile("sandbox.ts");
		var seen = new HashSet<string>(alreadyEmittedTypes.Select(GetTsTypeName));
		var toEmit = new HashSet<Type>();
		foreach (var t in SandboxWireTypes)
		{
			CollectReachableTypes(t, toEmit);
		}

		var ordered = toEmit
			.OrderBy(t => GetTsTypeName(t), StringComparer.Ordinal)
			.ToList();
		foreach (var t in ordered)
		{
			var name = GetTsTypeName(t);
			if (!seen.Add(name)) continue;
			var iface = BuildInterfaceForType(t);
			if (iface == null) continue;
			iface.AddModifier(TsModifier.Export);
			tsFile.AddDeclaration(iface);
		}

		// Hand-written discriminated union for SandboxEvent — the C# side ships
		// a single flat SandboxEventDto with a `type` discriminator, but Portal
		// is much happier with a real TS union it can switch on.
		AddSandboxEventUnion(tsFile);

		return new GeneratedFileDescriptor { FileName = "sandbox.ts", Content = tsFile.Render() };
	}

	private static void AddSandboxEventUnion(TsFile file)
	{
		void AddVariant(string name, string discriminator, params (string field, TsType type, bool optional)[] fields)
		{
			var iface = new TsInterface(name).AddModifier(TsModifier.Export);
			iface.AddProperty(new TsProperty("type", TsType.Of($"'{discriminator}'")));
			foreach (var (field, type, optional) in fields)
			{
				var prop = new TsProperty(field, type);
				if (optional) prop.AsOptional();
				iface.AddProperty(prop);
			}
			file.AddDeclaration(iface);
		}

		AddVariant("SandboxEventInvocationOutput", "invocation-output",
			("invocationId", TsType.String, false),
			("channel", TsType.String, false),
			("line", TsType.String, false));

		AddVariant("SandboxEventInvocationStatus", "invocation-status",
			("invocationId", TsType.String, false),
			("status", TsType.String, false),
			("exitCode", TsType.Number, true));

		AddVariant("SandboxEventFileChanged", "file-changed",
			("watchId", TsType.String, false),
			("path", TsType.String, false),
			("kinds", TsType.ArrayOf(TsType.String), false));

		AddVariant("SandboxEventConnection", "connection",
			("sessionId", TsType.String, false),
			("kind", TsType.String, false));

		AddVariant("SandboxEventShutdownImminent", "shutdown-imminent",
			("reason", TsType.String, false));

		var union = new TsTypeAlias("SandboxEvent").SetType(TsType.Union(
			TsType.Of("SandboxEventInvocationOutput"),
			TsType.Of("SandboxEventInvocationStatus"),
			TsType.Of("SandboxEventFileChanged"),
			TsType.Of("SandboxEventConnection"),
			TsType.Of("SandboxEventShutdownImminent")
		));
		union.AddModifier(TsModifier.Export);
		file.AddDeclaration(union);
	}

	private static GeneratedFileDescriptor GenerateVersionFile()
	{
		var tsFile = new TsFile("version.ts");
		var version = GetCliVersion();
		var versionAlias = new TsTypeAlias("BeamCliVersion").SetType(TsType.Of($"'{version}'"));
		versionAlias.AddModifier(TsModifier.Export);
		tsFile.AddDeclaration(versionAlias);

		// Runtime constant so consumers can do `if (BEAM_CLI_VERSION === '7.2.0')`.
		tsFile.AddDeclaration(new TsRawNode(
			$"export const BEAM_CLI_VERSION = '{version}' as const;\n"));

		return new GeneratedFileDescriptor { FileName = "version.ts", Content = tsFile.Render() };
	}

	private static GeneratedFileDescriptor GenerateIndexFile()
	{
		var tsFile = new TsFile("index.ts");
		tsFile.AddExport(new TsExport("./commands"));
		tsFile.AddExport(new TsExport("./sandbox"));
		tsFile.AddExport(new TsExport("./version"));
		return new GeneratedFileDescriptor { FileName = "index.ts", Content = tsFile.Render() };
	}

	private static string GetCliVersion()
	{
		try
		{
			return BeamAssemblyVersionUtil.GetVersion<TypeScriptCliGenerator>();
		}
		catch
		{
			return "0.0.0";
		}
	}

	private static TsInterface BuildArgsInterface(BeamCommandDescriptor cmd)
	{
		var name = GetArgsInterfaceName(cmd);
		var iface = new TsInterface(name);

		try
		{
			var args = cmd.command.Arguments.OrderBy(a => a.Name, StringComparer.Ordinal).ToList();
			var options = cmd.command.Options.OrderBy(o => o.Name, StringComparer.Ordinal).ToList();

			foreach (var arg in args)
			{
				var prop = new TsProperty(ToCamelCase(arg.Name), MapDotNetType(arg.ValueType, stringifyUnknown: true));
				if (arg.HasDefaultValue) prop.AsOptional();
				if (!string.IsNullOrWhiteSpace(arg.Description))
					prop.AddComment(new TsComment(arg.Description, TsCommentStyle.Doc));
				iface.AddProperty(prop);
			}

			foreach (var option in options)
			{
				var optType = option.ValueType;
				// System.CommandLine multi-value options may declare a scalar `string`
				// but accept many values via AllowMultipleArgumentsPerToken — only
				// wrap when the declared type isn't already a collection.
				if (option.AllowMultipleArgumentsPerToken && !IsCollectionType(optType))
				{
					optType = optType.MakeArrayType();
				}

				var prop = new TsProperty(ToCamelCase(option.Name), MapDotNetType(optType, stringifyUnknown: true));
				if (!option.IsRequired) prop.AsOptional();
				if (!string.IsNullOrWhiteSpace(option.Description))
					prop.AddComment(new TsComment(option.Description, TsCommentStyle.Doc));
				iface.AddProperty(prop);
			}
		}
		catch (Exception ex)
		{
			Log.Warning($"TypeScriptCliGenerator: skipping args for {cmd.executionPath} due to {ex.Message}");
			return null;
		}

		return iface;
	}

	private static TsInterface BuildInterfaceForType(Type t)
	{
		if (t.IsEnum) return null; // emitted as string union below by callers if needed
		if (IsPrimitiveOrBuiltin(t)) return null;
		if (t.IsArray) return null;
		if (t.IsGenericType) return null;

		var iface = new TsInterface(GetTsTypeName(t));
		try
		{
			var fields = UnityJsonContractResolver.GetSerializedFields(t)
				.OrderBy(f => f.Name, StringComparer.Ordinal)
				.ToList();
			foreach (var field in fields)
			{
				var fieldType = field.FieldType;
				var isOptional = false;
				if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					fieldType = fieldType.GetGenericArguments()[0];
					isOptional = true;
				}
				var prop = new TsProperty(field.Name, MapDotNetType(fieldType));
				if (isOptional) prop.AsOptional();
				iface.AddProperty(prop);
			}
		}
		catch (Exception ex)
		{
			Log.Warning($"TypeScriptCliGenerator: failed to introspect {t.FullName}: {ex.Message}");
			return null;
		}

		return iface;
	}

	private static void CollectReachableTypes(Type root, HashSet<Type> output)
	{
		var queue = new Queue<Type>();
		queue.Enqueue(root);
		while (queue.Count > 0)
		{
			var t = queue.Dequeue();
			if (t == null) continue;

			// Unwrap arrays / nullables / common generics
			if (t.IsArray)
			{
				queue.Enqueue(t.GetElementType());
				continue;
			}
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

			if (IsPrimitiveOrBuiltin(t)) continue;
			if (t.IsEnum) continue;
			if (!output.Add(t)) continue;

			try
			{
				foreach (var field in UnityJsonContractResolver.GetSerializedFields(t))
				{
					queue.Enqueue(field.FieldType);
				}
			}
			catch
			{
				// some types reject reflection; ignore.
			}
		}
	}

	private static TsType MapDotNetType(Type t, bool stringifyUnknown = false)
	{
		if (t == null) return TsType.Any;
		if (t == typeof(string) || t == typeof(char) || t == typeof(Guid)) return TsType.String;
		if (t == typeof(bool)) return TsType.Boolean;
		if (t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort)
		    || t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong)
		    || t == typeof(float) || t == typeof(double) || t == typeof(decimal))
			return TsType.Number;
		if (t == typeof(DateTime) || t == typeof(DateTimeOffset)) return TsType.String;
		if (t == typeof(void)) return TsType.Void;
		if (t == typeof(object)) return TsType.Unknown;

		if (t.IsArray) return TsType.ArrayOf(MapDotNetType(t.GetElementType(), stringifyUnknown));

		if (t.IsGenericType)
		{
			var gen = t.GetGenericTypeDefinition();
			var args = t.GetGenericArguments();
			if (gen == typeof(Nullable<>)) return MapDotNetType(args[0], stringifyUnknown);
			if (gen == typeof(List<>) || gen == typeof(IEnumerable<>) || gen == typeof(IList<>))
				return TsType.ArrayOf(MapDotNetType(args[0], stringifyUnknown));
			if (gen == typeof(Dictionary<,>))
				return TsType.Generic("Record", TsType.String, MapDotNetType(args[1], stringifyUnknown));
		}

		if (t.IsEnum) return TsType.String; // simplification — emit as bare string
		if (t == typeof(FileInfo) || t == typeof(DirectoryInfo)) return TsType.String;

		// CLI arg/option types that don't have a built-in mapping (e.g. a custom
		// Argument<ServiceName>) are string-convertible on the wire — surface them
		// as `string` rather than referencing a TS type we never emit.
		if (stringifyUnknown) return TsType.String;

		return TsType.Of(GetTsTypeName(t));
	}

	private static bool IsCollectionType(Type t)
	{
		if (t.IsArray) return true;
		if (!t.IsGenericType) return false;
		var gen = t.GetGenericTypeDefinition();
		return gen == typeof(IEnumerable<>) || gen == typeof(List<>) || gen == typeof(IList<>);
	}

	private static bool IsPrimitiveOrBuiltin(Type t)
	{
		if (t.IsPrimitive) return true;
		if (t == typeof(string) || t == typeof(char) || t == typeof(Guid)) return true;
		if (t == typeof(decimal)) return true;
		if (t == typeof(DateTime) || t == typeof(DateTimeOffset)) return true;
		if (t == typeof(void) || t == typeof(object)) return true;
		if (t == typeof(FileInfo) || t == typeof(DirectoryInfo)) return true;
		return false;
	}

	private static string GetTsTypeName(Type t)
	{
		if (t.IsArray) return GetTsTypeName(t.GetElementType()) + "Array";
		if (t.IsGenericType)
		{
			var gen = t.GetGenericTypeDefinition();
			var args = t.GetGenericArguments();
			if (gen == typeof(Nullable<>)) return GetTsTypeName(args[0]);
		}
		// Strip generics and inner-class delimiters
		var name = t.Name;
		var tickIdx = name.IndexOf('`');
		if (tickIdx >= 0) name = name.Substring(0, tickIdx);
		return name.Replace("+", "_");
	}

	private static string GetArgsInterfaceName(BeamCommandDescriptor cmd)
	{
		var parts = cmd.executionPath.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		var pascal = string.Concat(parts.Select(p =>
			string.Concat(p.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(Capitalize))));
		return pascal + "Args";
	}

	private static string ToCamelCase(string kebab)
	{
		if (string.IsNullOrEmpty(kebab)) return kebab;
		// Strip leading -- if present
		var s = kebab.TrimStart('-');
		var parts = s.Split('-', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0) return s;
		var head = parts[0].ToLowerInvariant();
		var tail = string.Concat(parts.Skip(1).Select(Capitalize));
		return head + tail;
	}

	private static string Capitalize(string s) =>
		string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
}

/// <summary>
/// Escape hatch so we can drop raw TS source into a TsFile without inventing
/// a new node type for one-line `export const` declarations.
/// </summary>
internal class TsRawNode : TsNode
{
	private readonly string _raw;
	public TsRawNode(string raw) { _raw = raw; }
	public override void Write(TsCodeWriter writer) => writer.Write(_raw);
}
