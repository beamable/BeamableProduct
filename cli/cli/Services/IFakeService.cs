using Beamable.Common;
using Beamable.Common.Api;

namespace cli;

public interface IFakeService
{
	int Add(int a, int b);

	Task<int> AddAsync(int a, int b);
}

public class FakeService : IFakeService
{
	private readonly IAppContext _ctx;
	private readonly IBeamableRequester _requester;

	public FakeService(IAppContext ctx, IBeamableRequester requester)
	{
		_ctx = ctx;
		_requester = requester;
		// _requester.Request<EmptyResponse>(Method.GET, "sdf");
	}

	public int Add(int a, int b)
	{
		return a + b;
	}

	public async Task<int> AddAsync(int a, int b)
	{
		BeamableLogger.Log("Async! " + _ctx.IsDryRun + " / " + _ctx.Cid);
		await _requester.Request<EmptyResponse>(Method.GET, "/basic/add");
		await Task.Delay(100);
		return a + b;
	}
}
