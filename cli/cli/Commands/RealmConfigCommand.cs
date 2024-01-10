using Beamable.Common.Api.Realms;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;

namespace cli;

public class RealmConfigCommandArgs : CommandArgs
{
	public bool plainOutput;
	public List<string> namespaces = new();
}

public class RealmConfigCommand : AppCommand<RealmConfigCommandArgs>, IResultSteam<DefaultStreamResultChannel, RealmConfigData>
{
	public RealmConfigCommand() : base("realm", "Get current realm config values") { }

	public override void Configure()
	{
		AddOption(new PlainOutputOption(), (args, b) => args.plainOutput = b);
		AddOption(new RealmConfigNamespaceOption(),(args, b) => args.namespaces = b.ToList());
	}

	public override async Task Handle(RealmConfigCommandArgs args)
	{
		try
		{
			var data = await args.RealmsApi.GetRealmConfig();
			this.SendResults(data);
			var json = JsonConvert.SerializeObject(data.ConvertToView(args.namespaces));
			if (args.plainOutput)
			{
				AnsiConsole.WriteLine(json);
			}
			else
			{
				AnsiConsole.Write(
					new Panel(new JsonText(json))
						.Header("Server response")
						.Collapse()
						.RoundedBorder());
			}
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
