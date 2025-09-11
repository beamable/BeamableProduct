using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using System.Reflection;

namespace beamable.otel.exporter.Serialization;

[Serializable]
public class LogsBatch
{
	public List<SerializableLogRecord> AllRecords { get; set; }
	public Dictionary<string, string> ResourceAttributes { get; set; }
	public int SchemaVersion { get; set; }
}

[Serializable]
public class SerializableLogRecord
{
	public string Timestamp { get; set; }
	public string CategoryName { get; set; }
	public LogLevel LogLevel { get; set; }
	public string FormattedMessage { get; set; }
	public string Body { get; set; }
	public ExceptionInfo Exception { get; set; }
	public Dictionary<string, string> Attributes { get; set; }
	public string TraceId { get; set; }
	public string SpanId { get; set; }
	public string TraceFlags { get; set; }
}

[Serializable]
public class ExceptionInfo
{
	public string Type { get; set; }
	public string Message { get; set; }
	public string StackTrace { get; set; }
}

public static class LogRecordSerializer
{
	private static readonly ConstructorInfo? _logRecordCtor;

	static LogRecordSerializer()
	{
		var constructors = typeof(LogRecord)
			.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);

		foreach (var con in constructors)
		{
			//TODO change this for an assertion to make sure the constructor that we got is the right one
			if (con.GetParameters().Length == 9) //This is to make sure we get the correct constructor
			{
				_logRecordCtor = con;
			}
		}

		if (_logRecordCtor == null)
			throw new InvalidOperationException("LogRecord constructor not found");
	}

	public static SerializableLogRecord SerializeLogRecord(LogRecord record)
	{
		return new SerializableLogRecord
		{
			Timestamp = record.Timestamp.ToString("o"),
			CategoryName = record.CategoryName ?? "",
			LogLevel = record.LogLevel,
			FormattedMessage = record.FormattedMessage ?? "",
			Body = record.Body ?? "",
			Exception = record.Exception is not null ? new ExceptionInfo
			{
				Type = record.Exception.GetType().FullName,
				Message = record.Exception.Message,
				StackTrace = record.Exception.StackTrace ?? ""
			} : null,
			Attributes = record.Attributes?.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString()),
			TraceId = record.TraceId.ToString(),
			SpanId = record.SpanId.ToString(),
			TraceFlags = record.TraceFlags.ToString()
		};
	}

	public static LogRecord DeserializeLogRecord(SerializableLogRecord serializedLog)
	{ 
		var timestamp = DateTimeOffset.Parse(serializedLog.Timestamp).UtcDateTime;
		var category = serializedLog.CategoryName;
		var logLevel = serializedLog.LogLevel;
		var eventId = null as object;
		var state = null as object;
		var body = serializedLog.Body;

		var attributes = serializedLog.Attributes?.Select(kv => new KeyValuePair<string, object>(kv.Key, kv.Value)).ToList()
		                 ?? new List<KeyValuePair<string, object>>();

		Exception exception = null;
		if (serializedLog.Exception != null)
		{
			exception = new Exception(serializedLog.Exception.Message);
		}

		var fields = new object[]
		{
			null,
			timestamp,
			category,
			logLevel,
			eventId,
			body,
			state,
			exception,
			attributes
		};

		return (LogRecord)_logRecordCtor.Invoke(fields);
	}
}
