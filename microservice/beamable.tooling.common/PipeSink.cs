using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text;

namespace Beamable.Server.Common;

public class PipeSink : ILogEventSink
{
	private readonly ITextFormatter _formatProvider;
	public List<string> messages = new List<string>();
	private long _messageCount = 0;
	private const int MAX_MESSAGE_BUFFER = 5;
	private readonly StringWriter _writer;
	
	public PipeSink(string cid, string pid, string serviceName, ITextFormatter formatProvider)
	{
		_writer = new StringWriter();
		
		_formatProvider = formatProvider;
	}

	public bool TryGetNextMessage(ref int pointer, out string message)
	{
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
