using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace cli;

public class BaseRequestDeleteCommand : BaseRequestCommand
{
	public override bool IsForInternalUse => true;
	public override int Order => 200;

	public BaseRequestDeleteCommand() : base("delete", "Base DELETE request command")
	{
	}
	protected override Method Method => Method.DELETE;

}
