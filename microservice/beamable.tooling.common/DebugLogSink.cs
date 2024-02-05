using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System.Collections.Concurrent;

namespace Beamable.Server.Common;

/// <summary>
/// Custom log sink that captures log messages and provides a way to retrieve them.
/// </summary>
public class DebugLogSink : ILogEventSink
{
	/// <summary>
	/// List to store captured log messages.
	/// </summary>
	public ConcurrentQueue<string> messages = new ConcurrentQueue<string>(); 

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

	/// <summary>
	/// Attempt to get the next log message.
	/// If the max message buffer is set to 25,
	/// And there have been 30 messages, then the first 5 messages will be lost.
	/// In this example, the first call to this method will return the 5th message (which is index=0)
	/// </summary>
	/// <param name="pointer">A reference pointer counting up the log message count. The expected use case is that the caller dedicate a ref int for this usage.</param>
	/// <param name="message">The next log message if there is one, null otherwise.</param>
	/// <returns>True if there is a log message, false otherwise.</returns>
	public bool TryGetNextMessage(ref int pointer, out string message)
	{
		if (messages.TryDequeue(out message))
		{
			pointer++;
			return true;
		}

		message = null;
		return false;
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
		messages.Enqueue(str);
		_messageCount++;
		
		while (messages.Count > MAX_MESSAGE_BUFFER)
		{
			if(messages.TryDequeue(out _))
			{
				_messageCount--;
			}
		}
	}
}
