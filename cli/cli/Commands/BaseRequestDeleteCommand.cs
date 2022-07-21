using Beamable.Common.Api;

namespace cli;

public class BaseRequestDeleteCommand : BaseRequestCommand
{
	public BaseRequestDeleteCommand(CliRequester requester) : base(requester, "delete", "base DELETE request command") { }
	protected override Method Method => Method.DELETE;
	
}
