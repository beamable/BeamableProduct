namespace cli;

public interface IFakeService
{
	int Add(int a, int b);

	Task<int> AddAsync(int a, int b);
}

public class FakeService : IFakeService
{
	private readonly IAppContext _ctx;

	public FakeService(IAppContext ctx)
	{
		_ctx = ctx;
	}

	public int Add(int a, int b)
	{
		return a + b;
	}

	public async Task<int> AddAsync(int a, int b)
	{
		Console.WriteLine("Async! " + _ctx.IsDryRun + " / " + _ctx.Cid);
		await Task.Delay(100);
		return a + b;
	}
}
