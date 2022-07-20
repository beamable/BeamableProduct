using Beamable.Common.Api;
using Spectre.Console;
using System.CommandLine;

namespace cli;

public abstract class BaseRequestCommand : AppCommand<BaseRequestArgs>
{
	protected abstract Method Method { get; }
	private readonly IBeamableRequester _requester;

	protected BaseRequestCommand(IBeamableRequester requester, string name, string description) : base(name, description)
	{
		_requester = requester;
	}
	public override void Configure()
	{
		var uri = new Argument<string>(nameof(BaseRequestArgs.uri));
		AddArgument(uri, (args, i) => args.uri = i);
		AddOption(new HeaderOption(), (args, i) => args.customHeaders.AddRange(i));
		AddOption(new BodyPathOption(), (args, i) => args.bodyPath = i);
	}

	public override async Task Handle(BaseRequestArgs args)
	{
		foreach (string customHeader in args.customHeaders)
		{
			Console.WriteLine($"HEADER: {customHeader}");
		}

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

			                                            await  _requester.Request(Method, args.uri, body,true,
				                                            s => s)
		                                );
		Console.WriteLine(response);
	}
}

public class BaseRequestArgs : CommandArgs
{
	public string uri;
	public List<string> customHeaders = new List<string>();
	public string bodyPath;
}
