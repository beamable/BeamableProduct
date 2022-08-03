using Serilog;

namespace cli;

public class GenerateSdkCommandArgs : CommandArgs
{
	/*
	 * need
	 */
}
public class GenerateSdkCommand : AppCommand<GenerateSdkCommandArgs>
{
	private readonly SwaggerService _swagger;

	public GenerateSdkCommand(SwaggerService swagger) : base("generate", "generate Beamable client source code from open API documents")
	{
		_swagger = swagger;
	}

	public override void Configure()
	{

	}

	public override async Task Handle(GenerateSdkCommandArgs args)
	{
		var output = await _swagger.Generate();
		foreach (var file in output)
		{
			Log.Warning(file.FileName);
			Log.Warning(file.Content);
		}
	}
}
