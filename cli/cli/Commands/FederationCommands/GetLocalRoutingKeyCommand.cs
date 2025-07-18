using Beamable.Common.Api;
using cli.Utils;

namespace cli.FederationCommands;

public class GetLocalRoutingKeyCommandArgs : CommandArgs
{
	
}

public class GetLocalRoutingKeyCommandOutput
{
	public string routingKey;
}

public class GetLocalRoutingKeyCommand : AtomicCommand<GetLocalRoutingKeyCommandArgs, GetLocalRoutingKeyCommandOutput>, ISkipManifest
{
	public GetLocalRoutingKeyCommand() : base("local-key", "Get the local routing key")
	{
	}

	public override void Configure()
	{
		
	}

	public override Task<GetLocalRoutingKeyCommandOutput> GetResult(GetLocalRoutingKeyCommandArgs args)
	{
		return Task.FromResult(new GetLocalRoutingKeyCommandOutput { routingKey = ServiceRoutingStrategyExtensions.GetDefaultRoutingKeyForMachine() });
	}
}
