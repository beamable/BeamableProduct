using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Server.Common;
using cli.Commands.Project;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using UnityEngine;

namespace cli.Services;

public class DataReporterService
{
	private readonly IAppContext _appContext;

	public DataReporterService(IAppContext appContext)
	{
		_appContext = appContext;
	}

	public void Report(string rawMessage)
	{
		if (_appContext.UsePipeOutput || _appContext.ShowRawOutput)
		{
			// std out
			Console.WriteLine(rawMessage);
		}
	}

	public void Report<T>(string type, T data)
	{
		var pt = new ReportDataPoint<T>
		{
			data = data,
			type = type,
			ts = DateTimeOffset.Now.ToUnixTimeMilliseconds()
		};
		try
		{
			var json = JsonConvert.SerializeObject(pt, UnitySerializationSettings.Instance);
			Report(json);
		}
		catch (Exception ex)
		{
			Console.WriteLine("ERROR: " + ex.GetType().Name);
			Log.Information(ex.Message);
			throw;
		}
	}
}
