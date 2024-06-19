using Beamable.Common.Dependencies;
using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;

namespace Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class DependencyProviderBenchmarks
{
	// [Benchmark]
	// public void JustAService()
	// {
	// 	var x = new ServiceDescriptor();
	// }
	// [Benchmark]
	// public void Dict_Concurrent()
	// {
	// 	var x = new ConcurrentDictionary<Type, ServiceDescriptor>();
	// }
	// [Benchmark]
	// public void Dict_Regular()
	// {
	// 	var x = new Dictionary<Type, ServiceDescriptor>();
	// }

	// [Benchmark]
	public void BaseCase_JustProvider()
	{
		var provider = new DependencyProvider(null, null);
	}
	
	// [Benchmark]
	public void BaseCase_NoDispose()
	{
		var builder = new DependencyBuilder();
		// builder.AddSingleton<TestService>();
		var provider = builder.Build();
	
		// var service = provider.GetService<TestService>();
	}
	//
	[Benchmark]
	public void BaseCase_NoDispose_RegisterAndResolve()
	{
		var builder = new DependencyBuilder();
		// builder.AddSingleton<TestService>();
		var provider = builder.Build();
		// var service = provider.GetService<TestService>();
		// var serv = new TestService();
	}
	//
	//
	[Benchmark]
	public void BaseCase_Dispose()
	{
		var builder = new DependencyBuilder();
		// builder.AddSingleton<TestService>();
		var provider = builder.Build();
	
		// var service = provider.GetService<TestService>();
	
		provider.Dispose();
	}


	public class TestService
	{
		
	}
}
