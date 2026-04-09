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
	
	/// <summary>
	/// Creates a unique listener subscription for the given service name and try to get early channels by the <paramref name="baseName"/>.
	/// </summary>
	public (string uniqueKey, Channel<string> channel) CreateUniqueListenerSubscription(string baseName, int capacity = 1024)
	{
		lock (subscriberToLogs)
		{
			var uniqueKey = $"{baseName}_{Guid.NewGuid()}";
			var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(capacity)
			{
				AllowSynchronousContinuations = false,
				FullMode = BoundedChannelFullMode.DropOldest,
				SingleReader = true,
				SingleWriter = true
			});

			// Try to get early channels values to the new unique one
			if (subscriberToLogs.TryRemove(baseName, out var seedChannel))
			{
				while (seedChannel.Reader.TryRead(out var msg))
				{
					channel.Writer.TryWrite(msg);
				}
			}

			if (!subscriberToLogs.TryAdd(uniqueKey, channel))
			{
				throw new Exception("Failed to add listener log subscription");
			}

			return (uniqueKey, channel);
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
			var buffer = new ArrayBufferWriter<byte>();
			_formatter.FormatLogEntry(buffer, log);
			var result = Encoding.UTF8.GetString(buffer.WrittenMemory.Span);

			foreach (var kvp in subscriberToLogs)
			{
				kvp.Value.Writer.TryWrite(result);
			}
		}
	}

	/// <summary>
	/// Writes a pre-formatted message string directly to all active subscriber channels.
	/// Use this when the log entry is already formatted (e.g. from a custom ILogger provider).
	/// </summary>
	public void WriteRawMessage(string message)
	{
		if (string.IsNullOrEmpty(message))
			return;

		lock (subscriberToLogs)
		{
			foreach (var kvp in subscriberToLogs)
			{
				kvp.Value.Writer.TryWrite(message);
			}
		}
	}
}
