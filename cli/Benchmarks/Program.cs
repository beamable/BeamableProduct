using BenchmarkDotNet.Running;

namespace Benchmarks;

public class Program
{
	public static void Main(string[] args)
	{
		// BenchmarkRunner.Run<MultiThreadedAccessBenchmarks>();
		BenchmarkRunner.Run<DependencyProviderBenchmarks>();
		// BenchmarkRunner.Run<PromiseBenchmarks>();
		
	}
}
