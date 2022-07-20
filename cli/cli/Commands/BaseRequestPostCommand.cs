using Beamable.Common.Api;

namespace cli;

public class BaseRequestPostCommand : BaseRequestCommand
{
	public BaseRequestPostCommand(CliRequester requester) : base(requester, "post", "base POST request command") { }
	protected override Method Method => Method.POST;
	
}
