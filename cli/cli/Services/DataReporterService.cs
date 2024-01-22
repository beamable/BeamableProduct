using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Server.Common;
using cli.Commands.Project;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using UnityEngine;

namespace cli.Services;

public interface IDataReporterService
{
	void Report<T>(string type, T data);
}

public class DataReporterService : IDataReporterService
{
	private readonly IAppContext _appContext;

	private bool _alreadySentFirstMessage;
	
	public DataReporterService(IAppContext appContext)
	{
		_appContext = appContext;
		_alreadySentFirstMessage = false;
	}

	public void Report(string rawMessage)
	{
		if (!_appContext.UsePipeOutput && !_appContext.ShowRawOutput)
		{
			return;
		}
		
		// the reporter use stdout, so that messages may be easily piped into other processes. 

		if (_alreadySentFirstMessage)
		{
			// print out a delimiter.
			Console.WriteLine(Reporting.MESSAGE_DELIMITER);
		}
		
		Console.WriteLine(rawMessage);
		_alreadySentFirstMessage = true;
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
