using Beamable.Common.Api;
using Spectre.Console;
using System.CommandLine;

namespace cli;

public abstract class BaseRequestCommand : AppCommand<BaseRequestArgs>
{
	protected abstract Method Method { get; }
	private readonly CliRequester _requester;

	protected BaseRequestCommand(CliRequester requester, string name, string description) : base(name, description)
	{
		_requester = requester;
	}

	public override void Configure()
	{
		var uri = new Argument<string>(nameof(BaseRequestArgs.uri));
		AddArgument(uri, (args, i) => args.uri = i);
		AddOption(new HeaderOption(), (args, i) => args.customHeaders.AddRange(i));
		AddOption(new BodyPathOption(), (args, i) => args.bodyPath = i);
		AddOption(new CustomerScopedOption(), (args, i) => args.customerScoped = i);
	}

	public override async Task Handle(BaseRequestArgs args)
	{
		string body = null;
		if (!string.IsNullOrWhiteSpace(args.bodyPath))
		{
			var exists = File.Exists(args.bodyPath);
			if (!exists)
			{
				Console.WriteLine($"There is no file with path: {args.bodyPath}");
				throw new FileNotFoundException();
			}

			body = await File.ReadAllTextAsync(args.bodyPath);
		}

		var response = await AnsiConsole.Status()
		                                .Spinner(Spinner.Known.Default)
		                                .StartAsync("Sending Request...", async _ =>
			                                            await _requester.CustomRequest(Method, args.uri, body, true,
				                                            s => s, args.customerScoped, args.customHeaders)
		                                );
		Console.WriteLine(response);
	}
}

public class BaseRequestArgs : CommandArgs
{
	public List<string> customHeaders = new();
	public string uri;
	public string bodyPath;
	public bool customerScoped = false;
}
