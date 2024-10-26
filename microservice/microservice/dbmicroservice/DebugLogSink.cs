using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Channels;

namespace Beamable.Server.Common;

/// <summary>
/// Custom log sink that captures log messages and provides a way to retrieve them.
/// </summary>
public class DebugLogSink : ILogEventSink
{


	public ConcurrentDictionary<string, Channel<string>> subscriberToLogs =
		new ConcurrentDictionary<string, Channel<string>>();

	private readonly ITextFormatter _formatProvider;
	private int _messageCount = 0;
	private const int MAX_MESSAGE_BUFFER = 30;
	private readonly StringWriter _writer;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="DebugLogSink"/> class with the specified text formatter.
	/// </summary>
	/// <param name="formatProvider">The text formatter used to format log messages.</param>
	public DebugLogSink(ITextFormatter formatProvider)
	{
		_writer = new StringWriter();
		
		_formatProvider = formatProvider;
	}

	public void ReleaseSubscription(string name)
	{
		lock (subscriberToLogs)
		{
			subscriberToLogs.TryRemove(name, out _);
		}
	}
	
	public Channel<string> GetMessageSubscription(string name, int capacity = 1024)
	{
		lock (subscriberToLogs)
		{
			if (subscriberToLogs.TryGetValue(name, out var existing))
			{
				return existing;
			}

			var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(capacity)
			{
				AllowSynchronousContinuations = false,
				FullMode = BoundedChannelFullMode.DropOldest,
				SingleReader = true,
				SingleWriter = true
			});

			if (!subscriberToLogs.TryAdd(name, channel))
			{
				throw new Exception("Failed to add log subscription");
			}

			return channel;
		}
	}
	
	/// <summary>
	/// Emits a log event to the sink, capturing the log message.
	/// </summary>
	/// <param name="logEvent">The log event to be emitted.</param>
	public void Emit(LogEvent logEvent)
	{
		_writer.GetStringBuilder().Clear();
		_formatProvider.Format(logEvent, _writer);
		_writer.Flush();
		var str = _writer.GetStringBuilder().ToString();

		lock (subscriberToLogs)
		{
			foreach (var kvp in subscriberToLogs)
			{
				kvp.Value.Writer.TryWrite(str);
			}
		}
		
		
	}
}
