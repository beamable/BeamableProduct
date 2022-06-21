using Newtonsoft.Json;

namespace cli;

public class ConfigCommandArgs : CommandArgs
{

}

public class ConfigCommand : AppCommand<ConfigCommandArgs>
{
	private readonly IAppContext _ctx;
	private readonly ConfigService _configService;

	public ConfigCommand(IAppContext ctx, ConfigService configService) : base("config", "list the current configuration")
	{
		_ctx = ctx;
		_configService = configService;
	}

	public override void Configure()
	{
		// nothing to do.
	}

	public override Task Handle(ConfigCommandArgs args)
	{
		var ctx = JsonConvert.SerializeObject(_ctx);
		Console.WriteLine(_configService.ConfigFilePath);
		Console.WriteLine(ctx);
		return Task.CompletedTask;
	}
}