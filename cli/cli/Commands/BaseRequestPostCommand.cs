using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace cli;

public class BaseRequestPostCommand : BaseRequestCommand
{
	public BaseRequestPostCommand() : base( "post", "base POST request command") { }
	protected override Method Method => Method.POST;

}
