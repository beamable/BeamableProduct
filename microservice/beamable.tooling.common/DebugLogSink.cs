using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Beamable.Server.Common;

public class DebugLogSink : ILogEventSink
{
	private readonly ITextFormatter _formatProvider;
	public List<string> messages = new List<string>();
	private int _messageCount = 0;
	private const int MAX_MESSAGE_BUFFER = 5;
	private readonly StringWriter _writer;
	
	public DebugLogSink(ITextFormatter formatProvider)
	{
		_writer = new StringWriter();
		
		_formatProvider = formatProvider;
	}

	/// <summary>
	/// Attempt to get the next log message.
	///
	/// If the max message buffer is set to 25,
	/// And there have been 30 messages, then the first 5 messages will be lost.
	/// In this example, the first call to this method will return the 5th message (which is index=0)
	/// 
	/// </summary>
	/// <param name="pointer">A reference pointer counting up the log message count. The expected use case is that the caller dedicate a ref int for this usage.</param>
	/// <param name="message">The next log message if there is one, null otherwise.</param>
	/// <returns>True if there is a log message, false otherwise.</returns>
	public bool TryGetNextMessage(ref int pointer, out string message)
	{
		if (_messageCount - MAX_MESSAGE_BUFFER > pointer)
		{
			pointer = _messageCount - MAX_MESSAGE_BUFFER;
		}
		
		if (pointer < _messageCount)
		{
			var index = pointer % MAX_MESSAGE_BUFFER;
			message = messages[index];
			pointer++;
			return true;
		}

		message = null;
		return false;
	}

	public void Emit(LogEvent logEvent)
	{
		_writer.GetStringBuilder().Clear();
		_formatProvider.Format(logEvent, _writer);
		_writer.Flush();
		var str = _writer.GetStringBuilder().ToString();
		messages.Add(str);
		_messageCount++;
		
		while (messages.Count > MAX_MESSAGE_BUFFER)
		{
			messages.RemoveAt(0);
		}
	}
}
