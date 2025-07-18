using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using System.Diagnostics;

namespace beamable.otel.exporter.Serialization;

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

	// public static LogRecord DeserializeLogRecord(SerializableLogRecord serializedLog)
	// {
	// 	var timestamp = DateTimeOffset.Parse(serializedLog.Timestamp);
	// 	var traceId = ActivityTraceId.CreateFromString(serializedLog.TraceId.AsSpan());
	// 	var spanId = ActivitySpanId.CreateFromString(serializedLog.SpanId.AsSpan());
	// 	var traceFlags = (ActivityTraceFlags)Enum.Parse(typeof(ActivityTraceFlags), serializedLog.TraceFlags);
	//
	// 	var attributes = serializedLog.Attributes?.Select(kv => new KeyValuePair<string, object>(kv.Key, kv.Value)).ToList()
	// 	                 ?? new List<KeyValuePair<string, object>>();
	//
	// 	Exception exception = null;
	// 	if (serializedLog.Exception != null)
	// 	{
	// 		exception = new Exception(serializedLog.Exception.Message); // Simple wrapping
	// 	}
	//
	// 	return new LogRecord(
	// 		timestamp: timestamp,
	// 		categoryName: serializedLog.CategoryName,
	// 		logLevel: serializedLog.LogLevel,
	// 		eventId: default,
	// 		state: null,
	// 		stateValues: null,
	// 		exception: exception,
	// 		traceId: traceId,
	// 		spanId: spanId,
	// 		traceFlags: traceFlags,
	// 		body: serializedLog.Body,
	// 		attributes: attributes,
	// 		scopeProvider: null
	// 	);
	// }
}
