using Beamable.Common.BeamCli;
using Beamable.Server.Common;
using Newtonsoft.Json;
using Serilog;

namespace cli.Services;

public interface IDataReporterService
{
	void Report<T>(string type, T data);
}

public static class IDataReporterServiceExtensions
{

	public static void Exception(this IDataReporterService reporter, Exception ex, int exitCode, string invocationContext)
	{
		ErrorOutput result = null;
		string channel = null;
		switch (ex)
		{
			case CliException cliEx: // perhaps custom output.
				result = cliEx.GetPayload(exitCode, invocationContext);
				channel = CliException.GetChannelName(result.GetType());
				break;
			default: // general uncaught exception
				result = new ErrorOutput();
				CliException.Apply(ex, ref result, exitCode, invocationContext);
				channel = DefaultErrorStream.CHANNEL;
				break;
		}
		reporter.Report(channel, result);
	}
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
			Console.Out.WriteLine(Reporting.MESSAGE_DELIMITER);
		}

		Console.Out.WriteLine(rawMessage);
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
			Log.Error($"Error: {ex.GetType().Name} - {ex.Message} -- {ex.StackTrace}");
			Log.Information(ex.Message);
			throw;
		}
	}
}
