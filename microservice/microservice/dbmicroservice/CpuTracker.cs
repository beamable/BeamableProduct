using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace Beamable.Server;

public class CpuTracker
{
	private bool isSampling;
	private DateTime startTime;
	private TimeSpan startCpuUsage;
	private Stopwatch stopWatch;

	private double[] _window;
	private int _windowIndex;
	public readonly int RollingAverageWindowSize;
	public bool Readable { get; private set; }
	
	public double RollingRatio { get; private set; }

	public CpuTracker(int windowSize = 4)
	{
		RollingAverageWindowSize = windowSize;
		_window = new double[RollingAverageWindowSize];
	}
	
	public void StartSample()
	{
		if (isSampling) throw new InvalidOperationException("Cannot start sample while sample is running");
		isSampling = true;
		startTime = DateTime.UtcNow;
		startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
		stopWatch = new Stopwatch();

		stopWatch.Start();	
	}

	public void EndSample()
	{
		if (!isSampling) throw new InvalidOperationException("Cannot end sample when no sample has been started.");
		isSampling = false;
		
		stopWatch.Stop();
		var endTime = DateTime.UtcNow;
		var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

		var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
		var totalMsPassed = (endTime - startTime).TotalMilliseconds;
		var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

		var cpuUsagePercentage = cpuUsageTotal;

		_window[_windowIndex] = cpuUsagePercentage;
		_windowIndex = (_windowIndex + 1) % RollingAverageWindowSize;
		if (!Readable && _windowIndex == 0) Readable = true;
		var sum = 0.0;
		for (var i = 0; i < RollingAverageWindowSize; i++)
		{
			sum += _window[i];
		}

		RollingRatio = Math.Clamp(sum / RollingAverageWindowSize, 0, 1);
	}
	
}
