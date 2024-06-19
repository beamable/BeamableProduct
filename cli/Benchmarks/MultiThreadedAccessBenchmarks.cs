using Beamable.Common.Dependencies;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class MultiThreadedAccessBenchmarks
{
	[Benchmark]
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
		
		
	}

	public class A
	{
		// no-op class just for testing
	}
}
