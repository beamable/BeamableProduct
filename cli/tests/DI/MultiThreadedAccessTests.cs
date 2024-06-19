using Beamable.Common.Dependencies;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace tests.DI;

public class MultiThreadedAccessTests
{
	[Test]
	public async Task SingletonOnlyGetsMadeOnce()
	{
		var builder = new DependencyBuilder();
		int count = 0;
		builder.AddSingleton<A>(_ =>
		{
			count++;
			return new A();
		});
		var provider = builder.Build();

		var tasks = Enumerable.Range(0, 10_000).Select(i => Task.Run(async () =>
		{
			await Task.Delay(1);
			provider.GetService<A>();
		}));

		await Task.WhenAll(tasks);
		
		Assert.That(count, Is.EqualTo(1), "the factory function should only be invoked once.");
	}

	public class A
	{
		// no-op class just for testing
	}
}
