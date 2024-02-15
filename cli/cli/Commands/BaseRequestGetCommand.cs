using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace cli;

public class BaseRequestGetCommand : BaseRequestCommand
{
	public override bool IsForInternalUse => true;
	public override int Order => 200;

	public BaseRequestGetCommand() : base("get", "Base GET request command")
	{
	}
	protected override Method Method => Method.GET;
}
