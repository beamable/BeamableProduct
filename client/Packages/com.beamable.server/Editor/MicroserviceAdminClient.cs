using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Server.Editor.DockerCommands;
using System;
using UnityEditor;

namespace Beamable.Server.Editor
{
	public class MicroserviceAdminClient
	{
		private readonly MicroserviceDescriptor _descriptor;
		private readonly IHttpRequester _httpRequester;

		public MicroserviceAdminClient(MicroserviceDescriptor descriptor, IHttpRequester httpRequester)
		{
			_descriptor = descriptor;
			_httpRequester = httpRequester;
		}

		public async Promise<string> GetAddress()
		{
			var comm = new DockerPortCommand(_descriptor, Constants.Features.Services.HEALTH_PORT);
			var res = await comm.Start(null);
			return res.ContainerExists ? $"http://{res.LocalAddress}":null;
		}

		public async Promise<bool> RebuildRouteTable()
		{
			var addr = await GetAddress();
			if (string.IsNullOrEmpty(addr))
			{
				return false;
			}
			var res = await _httpRequester.ManualRequest<string>(Method.POST, addr + "/routes/scan", parser: x=> x);
			return res.Contains("Rebuilt");
		}

		public async Promise<bool> IsHealthy()
		{
			var addr = await GetAddress();
			if (string.IsNullOrEmpty(addr))
			{
				return false;
			}

			var res = await _httpRequester.ManualRequest<string>(Method.GET, addr + "/health", parser: x=> x);

			if (res.Contains("Health"))
			{
				return true;
			}

			return false;
		}
	}
}
