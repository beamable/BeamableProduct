using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using ZLogger;

namespace Beamable.Server.Common;

public class DebugLogProcessor : IAsyncLogProcessor
{
	
	public ConcurrentDictionary<string, Channel<string>> subscriberToLogs =
		new ConcurrentDictionary<string, Channel<string>>();

	private IZLoggerFormatter _formatter;


	public DebugLogProcessor(ZLoggerOptions options)
	{
		_formatter = options.CreateFormatter();
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
	
	public ValueTask DisposeAsync()
	{
		return new ValueTask();
	}

	public void Post(IZLoggerEntry log)
	{
		lock (subscriberToLogs)
		{
			foreach (var kvp in subscriberToLogs)
			{
				var buffer = new ArrayBufferWriter<byte>();
				_formatter.FormatLogEntry(buffer, log);
				var result = Encoding.UTF8.GetString(buffer.WrittenMemory.Span);
				kvp.Value.Writer.TryWrite(result);
			}
		}
	}
}
