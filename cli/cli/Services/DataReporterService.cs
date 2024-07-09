using Beamable.Common.BeamCli;
using Beamable.Server.Common;
using Newtonsoft.Json;
using Serilog;

namespace cli.Services;

public interface IDataReporterService
{
	void Report<T>(string type, T data);

	void Exception(Exception ex, int exitCode, string invocationContext);
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

	public void Exception(Exception ex, int exitCode, string invocationContext)
	{
		var result = new ErrorOutput
		{
			exitCode = exitCode,
			invocation = invocationContext,
			message = ex?.Message,
			stackTrace = ex?.StackTrace,
			typeName = ex?.GetType().Name,
			fullTypeName = ex?.GetType().FullName
		};
		Report(GetChannelNameFromException(ex), result);
	}

	public static object GetExceptionPayload(Exception ex, int exitCode, string invocationContext)
	{
		if (ex is CliException cliException)
		{
			return cliException.GetPayload(exitCode, invocationContext);
		}
		else
		{
			return new ErrorOutput
			{
				exitCode = exitCode,
				invocation = invocationContext,
				message = ex?.Message,
				stackTrace = ex?.StackTrace,
				typeName = ex?.GetType().Name,
				fullTypeName = ex?.GetType().FullName
			};
		}
	}

	public static string GetChannelNameFromException(Exception ex)
	{
		if (ex is CliException)
		{
			// TODO: check if its special case...
		}
		else
		{
			return "uncaught-error";
		}
	}
}
