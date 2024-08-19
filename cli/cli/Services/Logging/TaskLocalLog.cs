using Beamable.Common.Dependencies;
using cli.Services;
using Serilog;
using Serilog.Events;

namespace cli;

public class TaskLocalLog : ILogger
{
	public static TaskLocalLog Instance = new TaskLocalLog();

	public AsyncLocal<ILogger> Context = new AsyncLocal<ILogger>();
	
	private TaskLocalLog()
	{
		
	}

	public void CreateContext(IDependencyProvider provider)
	{
		Context.Value = new NestedLogger(provider);
	}
	
	
	public ILogger globalLogger;
	
	public void Write(LogEvent logEvent)
	{
		globalLogger.Write(logEvent);
		if (Context.Value != null)
		{
			Context.Value.Write(logEvent);
		}
	}

	class NestedLogger : ILogger
	{
		private ReporterSink _sink;

		public NestedLogger(IDependencyProvider provider)
		{
			_sink = new ReporterSink(provider);
		}

		public void Write(LogEvent logEvent)
		{
			_sink.Emit(logEvent);
		}
	}
}
