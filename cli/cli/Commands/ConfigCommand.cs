using Newtonsoft.Json;

namespace cli;

public class ConfigCommandArgs : CommandArgs
{

}

public class ConfigCommand : AppCommand<ConfigCommandArgs>
{
	private readonly IAppContext _ctx;

	public ConfigCommand(IAppContext ctx) : base("config", "list the current configuration")
	{
		_ctx = ctx;
	}

	public override void Configure()
	{
		// nothing to do.
	}

	public override Task Handle(ConfigCommandArgs args)
	{
		var ctx = JsonConvert.SerializeObject(_ctx);
		Console.WriteLine(ctx);
		return Task.CompletedTask;
	}
}