using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace cli;

public class BaseRequestPostCommand : BaseRequestCommand
{
	public override bool IsForInternalUse => true;
	public override int Order => 200;

	public BaseRequestPostCommand() : base("post", "Base POST request command")
	{
	}
	protected override Method Method => Method.POST;

}
