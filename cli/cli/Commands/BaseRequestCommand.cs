using Beamable.Common;
using Beamable.Common.Api;
using cli.Utils;
using Spectre.Console;
using Spectre.Console.Json;
using System.CommandLine;

namespace cli;

public abstract class BaseRequestCommand : AppCommand<BaseRequestArgs>
{
	protected abstract Method Method { get; }
	private CliRequester _requester;

	protected BaseRequestCommand(string name, string description) : base(name, description)
	{
	}

	public override void Configure()
	{
		var uri = new Argument<string>(nameof(BaseRequestArgs.uri));
		AddArgument(uri, (args, i) => args.uri = i);
		AddOption(new HeaderOption(), (args, i) => args.customHeaders.AddRange(i));
		AddOption(new BodyPathOption(), (args, i) => args.bodyPath = i);
		AddOption(new CustomerScopedOption(), (args, b) => args.customerScoped = b);
		AddOption(new PlainOutputOption(), (args, b) => args.plainOutput = b);
	}

	public override async Task Handle(BaseRequestArgs args)
	{
		_requester = args.Requester;

		string body = null;
		if (!string.IsNullOrWhiteSpace(args.bodyPath))
		{
			var exists = File.Exists(args.bodyPath);
			if (!exists)
			{
				BeamableLogger.LogError($"There is no file with path: {args.bodyPath}");
				throw new FileNotFoundException();
			}

			body = await File.ReadAllTextAsync(args.bodyPath);
		}

		var response = await _requester.CustomRequest(Method, args.uri, body, true,
				s => s, args.customerScoped, args.customHeaders)
			.ShowLoading("Sending Request..");
		if (args.plainOutput)
		{
			AnsiConsole.WriteLine(response);
		}
		else
		{
			AnsiConsole.Write(
				new Panel(new JsonText(response))
					.Header($"{args.uri}")
					.Collapse()
					.RoundedBorder());
		}

	}
}

public class BaseRequestArgs : CommandArgs
{
	public List<string> customHeaders = new();
	public string uri;
	public string bodyPath;
	public bool customerScoped;
	public bool plainOutput;
}
