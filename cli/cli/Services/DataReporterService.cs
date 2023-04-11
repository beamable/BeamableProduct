using Beamable.Common.BeamCli;
using Beamable.Server.Common;
using Newtonsoft.Json;
using Serilog;
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
		if (!_appContext.UseFatalAsReportingChannel) return;

		var encoded = Reporting.EncodeMessage(rawMessage);
		Log.Fatal(encoded);
	}

	public void Report<T>(string type, T data)
	{
		var pt = new ReportDataPoint<T>
		{
			data = data, 
			type = type,
			ts = DateTimeOffset.Now.ToUnixTimeMilliseconds()
		};
		var json = JsonConvert.SerializeObject(pt, UnitySerializationSettings.Instance);
		Report(json);
	}
}
