using Beamable.Common;
using Beamable.Server.Api.Usage;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Beamable.Server.Ecs;

public class EcsService : IUsageApi
{
	private readonly HttpClient _client;
	private UsageData _latest;
	private double _cpuAverage;
	private double _memAverage;
	private int _sampleCount;
	private ContainerMetadata _containerMetadata;
	
	private string EcsUrl => Environment.GetEnvironmentVariable("ECS_CONTAINER_METADATA_URI_V4");
	
	public double CpuAverage => _cpuAverage;
	public double MemAverage => _memAverage;


	public EcsService(HttpClient client)
	{
		_client = client;
		Task.Factory.StartNew(Loop, TaskCreationOptions.LongRunning);
	}

	
	private async Task Loop()
	{
		var periodMs = TimeSpan.FromMilliseconds(1000 * 2); // every other second
		var averageResetPeriod = TimeSpan.FromMilliseconds(1000 * 20); // every 20 seconds
		var nextResetTime = DateTimeOffset.UtcNow + averageResetPeriod;
		while (true)
		{
			try
			{
				if (DateTimeOffset.UtcNow > nextResetTime)
				{
					_sampleCount = 0;
				}
				
				await Task.Delay(periodMs);

				var usage = await FetchCurrentUsage();
				_sampleCount++;
				_cpuAverage = RollAverage(_cpuAverage, usage.cpuUsage, _sampleCount);
				_memAverage = RollAverage(_memAverage, usage.memUsage, _sampleCount);
				
				_latest = usage;
			}
			catch (Exception ex)
			{
				// log the error, but continue
				BeamableLogger.LogWarning($"Ecs monitoring service error: {ex.GetType().Name}-{ex.Message}\n{ex.StackTrace}");
			}
		}
	}

	static double RollAverage(double oldAverage, double nextValue, int n)
	{
		return oldAverage * (n - 1) / n + nextValue / n;
	}
	

	private async Task<UsageData> FetchCurrentUsage()
	{
		
		var statsTask = GetContainerStats();
		var stats = await statsTask;
		
		var usage = new UsageData { data = stats, memUsage = stats.GetMemUsage(), cpuUsage = stats.GetCpuUsage() };
		return usage;
	}
	
	private async Task<ContainerMetadata> GetContainerMetadata()
	{
		var metadataStr = await _client.GetStringAsync(EcsUrl + "/task");
		var metadata = JsonConvert.DeserializeObject<ContainerMetadata>(metadataStr);
		return metadata;
	}
	
	private async Task<ContainerStatsResponse> GetContainerStats()
	{
		var data = await _client.GetStringAsync(EcsUrl + "/stats");
		var res = JsonConvert.DeserializeObject<ContainerStatsResponse>(data);
		return res;
	}

	public ServiceUsage GetUsage()
	{
		return new ServiceUsage
		{
			observedCpuAverage = CpuAverage,
			memoryAverage = MemAverage,
			latestCpuUsage = _latest.cpuUsage,
			latestMemoryUsage = _latest.memUsage
		};
	}

	public ServiceMetadata GetMetadata()
	{
		return new ServiceMetadata { instanceId = _containerMetadata.TaskARN, environment = ServiceEnvironment.Deployed };
	}

	public async Task Init()
	{
		_containerMetadata = await GetContainerMetadata();
	}
}


[Serializable]
public struct UsageData
{
	public double cpuUsage;
	public double memUsage;
	public ContainerStatsResponse data;

}

[Serializable]
public struct ContainerMetadata
{
	public string TaskARN;
	public string Revision;
	public ContainerLimits Limits;
}

[Serializable]
public struct ContainerLimits
{
	public double CPU;
	public double Memory;
}
	
[Serializable]
public struct ContainerStatsResponse
{
	public ContainerCpuStats cpu_stats;
	public ContainerCpuStats precpu_stats;
	public ContainerMemoryStats memory_stats;

	public double GetMemUsage()
	{
		return (memory_stats.usage / (double)memory_stats.limit) * 100;
	}
	
	public double GetCpuUsage()
	{
		double usage = 0;
		var cpuDelta = cpu_stats.cpu_usage.total_usage - precpu_stats.cpu_usage.total_usage;
		var sysDelta = cpu_stats.system_cpu_usage - precpu_stats.system_cpu_usage;
		if (sysDelta > 0 && cpuDelta > 0)
		{
			usage = (cpuDelta / (double)sysDelta) * (cpu_stats.cpu_usage.percpu_usage.Length) * 100;
		}

		return usage;
	}
}
    
[Serializable]
public struct ContainerCpuStats
{
	public ContainerCpuUsage cpu_usage;
	public long system_cpu_usage;
		
}
	
[Serializable]
public struct ContainerCpuUsage
{
	public long total_usage;
	public long usage_in_kernelmode;
	public long usage_in_usermode;
	public long[] percpu_usage;
}

[Serializable]
public struct ContainerMemoryStats
{
	public long usage;
	public long max_usage;
	public long limit;
}
