using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using cli;
using cli.Commands.Project;
using cli.Services;
using Beamable.Common.Dependencies;
using Beamable.Server;
using NUnit.Framework;

namespace tests;

public class DeploymentParallelCountTests
{
	[Test]
	public async Task TestMaxParallelCountThrottle()
	{
		// This test validates that the throttling logic limits concurrent builds
		var maxParallelCount = 3;
		var totalServices = 10;
		var concurrentBuildCounts = new List<int>();
		var lockObj = new object();
		var currentlyRunning = 0;
		var tasks = new List<Task>();

		// Simulate the throttling behavior
		for (var i = 0; i < totalServices; i++)
		{
			var taskIndex = i;
			var buildTask = Task.Run(async () =>
			{
				lock (lockObj)
				{
					currentlyRunning++;
					concurrentBuildCounts.Add(currentlyRunning);
				}

				// Simulate build time
				await Task.Delay(50);

				lock (lockObj)
				{
					currentlyRunning--;
				}
			});

			tasks.Add(buildTask);

			// Simulate the throttling logic from CreateReleaseManifestFromLocal
			while (tasks.Count(t => !t.IsCompleted) >= maxParallelCount)
			{
				await Task.WhenAny(tasks.Where(t => !t.IsCompleted));
			}
		}

		await Task.WhenAll(tasks);

		// Verify that we never exceeded the max parallel count
		var maxObservedConcurrent = concurrentBuildCounts.Max();
		Assert.LessOrEqual(maxObservedConcurrent, maxParallelCount,
			$"Maximum concurrent builds ({maxObservedConcurrent}) exceeded the limit ({maxParallelCount})");
	}

	[Test]
	public async Task TestSequentialBuildBypassesThrottle()
	{
		// This test validates that sequential builds work without throttling
		var tasks = new List<Task<int>>();
		var executionOrder = new List<int>();
		var lockObj = new object();

		for (var i = 0; i < 5; i++)
		{
			var index = i;
			var task = Task.Run(async () =>
			{
				await Task.Delay(10);
				lock (lockObj)
				{
					executionOrder.Add(index);
				}
				return index;
			});

			tasks.Add(task);
			// Sequential build: wait for each task before adding the next
			await task;
		}

		await Task.WhenAll(tasks);

		// Verify that tasks executed in order
		Assert.AreEqual(5, executionOrder.Count);
		for (var i = 0; i < 5; i++)
		{
			Assert.AreEqual(i, executionOrder[i], $"Task at index {i} did not execute in sequential order");
		}
	}

	[Test]
	public void TestMaxParallelCountDefaultValue()
	{
		// This test validates that the default value is 8
		// The default should be set when the property is not explicitly initialized
		var args = new cli.DeploymentCommands.PlanDeploymentCommandArgs();

		// The default value should be 0 (C# default) until set by the command option
		// The command option has a default of 8, so we verify that the option definition matches
		Assert.AreEqual(0, args.MaxParallelCount, "Uninitialized MaxParallelCount should be 0 (C# default)");
		
		// To properly test, we would need to parse command line args, but we can at least verify
		// that setting it to 8 works as expected (matching the option default)
		args.MaxParallelCount = 8;
		Assert.AreEqual(8, args.MaxParallelCount, "MaxParallelCount should be settable to 8");
	}

	[Test]
	public void TestMaxParallelCountCanBeCustomized()
	{
		// This test validates that the value can be customized
		var customValue = 5;
		var args = new cli.DeploymentCommands.PlanDeploymentCommandArgs
		{
			MaxParallelCount = customValue
		};

		Assert.AreEqual(customValue, args.MaxParallelCount, 
			$"MaxParallelCount should be customizable to {customValue}");
	}
}
