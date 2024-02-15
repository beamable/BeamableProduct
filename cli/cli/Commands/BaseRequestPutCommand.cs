using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace cli;

public class BaseRequestPutCommand : BaseRequestCommand
{
	public override bool IsForInternalUse => true;
	public override int Order => 200;

	public BaseRequestPutCommand() : base("put", "Base PUT request command")
	{
	}
	protected override Method Method => Method.PUT;
}
