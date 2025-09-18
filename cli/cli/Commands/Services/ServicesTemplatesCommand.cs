using cli.Services;
using cli.Utils;
using Spectre.Console;

namespace cli;

public class ServicesTemplatesCommandArgs : LoginCommandArgs
{
}

public class ServicesTemplatesCommandOutput
{
	public List<ServiceTemplate> templates;
}

public class ServicesTemplatesCommand : AtomicCommand<ServicesTemplatesCommandArgs, ServicesTemplatesCommandOutput>
{
	private BeamoService _remoteBeamo;


	public ServicesTemplatesCommand() :
		base("templates",
			ServicesDeletionNotice.REMOVED_PREFIX + "Gets all the template types available in this realm")
	{
	}

	public override void Configure()
	{
	}

	public override async Task<ServicesTemplatesCommandOutput> GetResult(ServicesTemplatesCommandArgs args)
	{
		AnsiConsole.MarkupLine(ServicesDeletionNotice.TITLE);
		AnsiConsole.MarkupLine(ServicesDeletionNotice.UNSUPPORTED_MESSAGE);
		throw CliExceptions.COMMAND_NO_LONGER_SUPPORTED;
	}
}
