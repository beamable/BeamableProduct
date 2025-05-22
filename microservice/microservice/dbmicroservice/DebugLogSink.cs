using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;
using ZLogger;

namespace Beamable.Server.Common;

public class DebugLogProcessor : IAsyncLogProcessor
{
	
	public ConcurrentDictionary<string, Channel<string>> subscriberToLogs =
		new ConcurrentDictionary<string, Channel<string>>();

	
	public DebugLogProcessor()
	{
		
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
		return ValueTask.CompletedTask;
	}

	public void Post(IZLoggerEntry log)
	{
		lock (subscriberToLogs)
		{
			foreach (var kvp in subscriberToLogs)
			{
				kvp.Value.Writer.TryWrite(log.ToString());
			}
		}
	}
}
