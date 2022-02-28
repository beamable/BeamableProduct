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

		public async Promise<string> GetCurrentToken()
		{
			var addr = await GetAddress();
			if (string.IsNullOrEmpty(addr))
			{
				return "";
			}
			var token = await _httpRequester.ManualRequest<string>(Method.GET, addr + "/token", parser: x=> x);
			return token;
		}

		public Promise ReloadCode()
		{

			// while something is not true; keep going...
			async Promise<bool> Ping()
			{
				var addr = await GetAddress();
				if (string.IsNullOrEmpty(addr))
				{
					throw new Exception("Not running");
				}

				var token = await GetCurrentToken();
				var entry = MicroserviceConfiguration.Instance.GetEntry(_descriptor.Name);
				var tokenMatch = string.Equals(entry.RobotId, token);
				UnityEngine.Debug.Log($"{_descriptor.Name} / {entry.RobotId} / {token}");
				return tokenMatch;
			}

			var p = new Promise();
			var nextTime = 0.0;
			void Tick()
			{
				if (EditorApplication.timeSinceStartup > nextTime)
				{
					EditorApplication.update -= Tick;
					Ping().Then(res =>
					{
						if (res)
						{
							UnityEngine.Debug.Log($"{_descriptor.Name} / match");

							p.CompleteSuccess();
						}
						else
						{
							nextTime = EditorApplication.timeSinceStartup + .15;
							EditorApplication.update += Tick;
						}
					}).Error(_ =>
					{
						UnityEngine.Debug.Log($"{_descriptor.Name} / not running");

						p.CompleteSuccess();
					});
				}
			}
			EditorApplication.update += Tick;
			return p;
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
