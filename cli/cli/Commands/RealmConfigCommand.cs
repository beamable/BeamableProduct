using Beamable.Common.Api.Realms;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;

namespace cli;

public class RealmConfigCommandArgs : CommandArgs
{
	public List<string> namespaces = new();
}

public class RealmConfigOutput
{
	public Dictionary<string, string> Config;
	public List<RealmConfigView> ConvertToView(List<string> namespaceFilter = default)
	{
		const char configSeparator = '|';
		var groupedConfig = new Dictionary<string, Dictionary<string, string>>();
		foreach (var keyValue in Config)
		{
			string namespaceKey = "";
			string configKey = "";

			if (keyValue.Key.Contains(configSeparator))
			{
				var keyParts = keyValue.Key.Split(configSeparator);
				namespaceKey = keyParts[0];
				configKey = keyParts[1];
			}
			else
			{
				configKey = keyValue.Key;
			}


			if (namespaceFilter?.Count > 0 && !namespaceFilter.Contains(namespaceKey))
			{
				continue;
			}

			if (!groupedConfig.ContainsKey(namespaceKey))
			{
				groupedConfig[namespaceKey] = new Dictionary<string, string>();
			}

			groupedConfig[namespaceKey][configKey] = keyValue.Value;
		}

		return groupedConfig.Select(pair => new RealmConfigView
		{
			Namespace = pair.Key,
			Config = pair.Value
		}).OrderBy(pair => pair.Namespace).ToList();
	}
}

public class RealmConfigCommand : AtomicCommand<RealmConfigCommandArgs, RealmConfigOutput>
{
	public override bool AutoLogOutput => false;
	public RealmConfigCommand() : base("realm", "Get current realm config values") { }

	protected override RealmConfigOutput GetHelpInstance()
	{
		return new RealmConfigOutput { Config = new Dictionary<string, string> { ["thor_rpc|useHttp"] = "true" } };
	}

	public override void Configure()
	{
		AddOption(new RealmConfigNamespaceOption(), (args, b) => args.namespaces = b.ToList());
	}

	public override async Task<RealmConfigOutput> GetResult(RealmConfigCommandArgs args)
	{
		try
		{
			var data = await args.RealmsApi.GetRealmConfig();
			var output = new RealmConfigOutput { Config = data.Config };
			LogResult(data.ConvertToView(args.namespaces));
			return output;
		}
		catch (Exception e)
		{
			throw new CliException($"Failed to get realm config data due to error: {e.Message}");
		}
	}
}

public class RealmConfigKeyValueData
{
	public string Namespace { get; set; } = String.Empty;
	public string Key { get; set; } = String.Empty;
	public string Value { get; set; } = String.Empty;
	public string NamespaceKey() => $"{Namespace}|{Key}";
}


public static class RealmConfigInputParser
{
	public static RealmConfigKeyValueData Parse(string input)
	{
		const string namespaceSeparator = "|";
		const string valueSeparator = "::";
		var result = new RealmConfigKeyValueData();
		try
		{
			if (!input.Contains(namespaceSeparator))
			{
				throw new ArgumentException("Invalid input format.");
			}
			var namespaceKeyParts = input.Split(namespaceSeparator);
			result.Namespace = namespaceKeyParts[0];
			if (namespaceKeyParts[1].Contains(valueSeparator))
			{
				var keyValueParts = namespaceKeyParts[1].Split(valueSeparator);
				result.Key = keyValueParts[0];
				result.Value = keyValueParts[1];
			}
			else
			{
				result.Key = namespaceKeyParts[1];
			}
		}
		catch (Exception)
		{
			throw new ArgumentException("Invalid input format.");
		}
		return result;
	}
}
