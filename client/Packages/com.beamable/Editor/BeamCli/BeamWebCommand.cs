using Beamable.Common;
using Beamable.Common.BeamCli;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using UnityEngine;

namespace Beamable.Editor.BeamCli
{

	public class BeamWebCommandFactory : IBeamCommandFactory
	{
		private HttpClient localClient = new HttpClient
		{
			Timeout = TimeSpan.FromDays(7)
		};
		
		public IBeamCommand Create()
		{
			return new BeamWebCommand(localClient);
		}

		public void ClearAll()
		{
			// TODO:
		}
	}

	[Serializable]
	public class BeamWebCommandRequest
	{
		public string commandLine;
	}
	
	public class BeamWebCommand : IBeamCommand
	{
		private string _command;
		private HttpClient _localClient;
		private Action<ReportDataPointDescription> _callbacks = (_) => { };


		public BeamWebCommand(HttpClient localClient)
		{
			_localClient = localClient;
		}

		public void SetCommand(string command)
		{
			_command = command.Substring("beam".Length);
		}

		public async Promise Run()
		{
			
			// TODO: we need to figure out the port, or start the server if its not running...
			
			var port = 8082;
			var req = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:{port}/execute");
			var json = JsonUtility.ToJson(new BeamWebCommandRequest {commandLine = _command});
			req.Content = new StringContent(json, Encoding.UTF8, "application/json");
			CliLogger.Log("Sending cli web request, " + json);
			using HttpResponseMessage response = await _localClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
			using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
			using StreamReader reader = new StreamReader(streamToReadFrom);

			while (!reader.EndOfStream)
			{
				var line = await reader.ReadLineAsync();
				
				if (string.IsNullOrEmpty(line)) continue;
				if (!line.StartsWith("data: ")) continue;
				
				var lineJson = line.Substring("data: ".Length);
				CliLogger.Log("received, " + lineJson, "from " + _command);

				var res = JsonUtility.FromJson<ReportDataPointDescription>(lineJson);
				res.json = lineJson;
				_callbacks?.Invoke(res);
			}
		}

		public IBeamCommand On<T>(string type, Action<ReportDataPoint<T>> cb)
		{
			On(desc =>
			{
				if (desc.type != type) return;
				var pt = JsonUtility.FromJson<ReportDataPoint<T>>(desc.json);
				cb?.Invoke(pt);
			});

			return this;
		}

		public IBeamCommand On(Action<ReportDataPointDescription> cb)
		{
			_callbacks += cb;
			return this;
		}

		public IBeamCommand OnError(Action<ReportDataPoint<ErrorOutput>> cb)
		{
			return On<ErrorOutput>("error", cb);
		}
	}
}
