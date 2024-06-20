using Beamable.Common;
using BenchmarkDotNet.Attributes;
using System.Runtime.InteropServices.JavaScript;

namespace Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class PromiseBenchmarks
{

	// [Benchmark]
	public void PromiseAllocation_Many()
	{
		for (var i = 0; i < 10_000; i++)
		{
			var p = new Promise();
		}
	} 
	
	// [Benchmark]
	public void PromiseAllocation()
	{
		var p = new Promise();
	} 
	
	
	[Benchmark]
	public Promise ReturnPromise()
	{
		var p = new Promise();
		return null;
	}
	
	[Benchmark]
	public async Promise ReturnAsyncPromise()
	{
		var p = new Promise();
	}
	
		
	// [Benchmark]
	public async Promise AsyncAwait2()
	{
		var p = new Promise();
		p.CompleteSuccess();
	}
	
	// [Benchmark]
	public async Task Sequence()
	{
		var pList = Enumerable.Range(0, 10).Select(_ => new Promise<int>()).ToList();
		var final = Promise.Sequence(pList);

		var _ = pList.Select(p => Task.Run(async () =>
		{
			await Task.Delay(1);
			p.CompleteSuccess(1);
		})).ToList();
		
		await final;
	}
	
	// [Benchmark]
	public async Task WhenAll()
	{
		var pList = Enumerable.Range(0, 10).Select(_ => new Promise<int>()).ToList();
		var final = Promise.WhenAll(pList);

		var _ = pList.Select(p => Task.Run(async () =>
		{
			await Task.Delay(1);
			p.CompleteSuccess(1);
		})).ToList();
		
		await final;
	}
}
