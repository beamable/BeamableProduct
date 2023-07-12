using Beamable.Common;
using Spectre.Console;

namespace cli.Utils;

public static class PromiseExtensions
{
	public static Promise<T> ShowLoading<T>(this Promise<T> self, string message = "loading...",
		Spinner? spinner = null)
	{
		spinner ??= Spinner.Known.Default;
		return AnsiConsole.Status()
			.Spinner(spinner)
			.StartAsync(message, async ctx => await self).ToPromise();
	}
}
