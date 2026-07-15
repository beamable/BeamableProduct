using System;

namespace cli.Services.Sandbox;

public interface IClock
{
	DateTimeOffset UtcNow { get; }
	long UnixTimeMs { get; }
}

public sealed class SystemClock : IClock
{
	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
	public long UnixTimeMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
