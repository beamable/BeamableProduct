using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace cli;

public class BaseRequestGetCommand : BaseRequestCommand
{
	public BaseRequestGetCommand(CliRequester requester) : base(requester,"get", "base GET request command") { }
	protected override Method Method => Method.GET;
}
