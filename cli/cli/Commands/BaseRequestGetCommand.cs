using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace cli;

public class BaseRequestGetCommand : BaseRequestCommand
{
	public BaseRequestGetCommand() : base("get", "Base GET request command") { }
	protected override Method Method => Method.GET;
}
