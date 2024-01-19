using Beamable.Server.Api.Usage;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace microservice.dbmicroservice;

public class LocalUsageService : IUsageApi
{
	public ServiceUsage GetUsage()
	{
		return new ServiceUsage();
	}

	public ServiceMetadata GetMetadata()
	{
		return new ServiceMetadata { environment = ServiceEnvironment.LocalStandalone, instanceId = Process.GetCurrentProcess().Id.ToString() };
	}

	public Task Init()
	{
		return Task.CompletedTask;
	}
}

public class DockerService : IUsageApi
{
	private ServiceMetadata _metadata;
	
	public ServiceUsage GetUsage()
	{
		return new ServiceUsage();
	}

	public ServiceMetadata GetMetadata()
	{
		return _metadata;
	}

	public async Task Init()
	{
		_metadata = new ServiceMetadata { environment = ServiceEnvironment.LocalDocker, instanceId = "" };
		
		var p = new Process();
		p.StartInfo = new ProcessStartInfo
			{
				FileName = "cat",
				Arguments = "/etc/hostname",
				RedirectStandardError = true,
				RedirectStandardOutput = true
			}
			;

		p.Start();
		await p.WaitForExitAsync();
		_metadata.instanceId = (await p.StandardOutput.ReadToEndAsync()).ReplaceLineEndings(String.Empty);
		
	}
}
