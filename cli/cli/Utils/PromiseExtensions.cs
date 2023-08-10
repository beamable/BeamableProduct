using Beamable.Common;
using JetBrains.Annotations;
using Spectre.Console;

namespace cli.Utils;

public static class PromiseExtensions
{
	public static Promise<T> ShowLoading<T>(this Promise<T> self, string message = "loading...",
		[CanBeNull] Spinner spinner = null)
	{
		spinner ??= Spinner.Known.Default;
		return AnsiConsole.Status()
			.Spinner(spinner)
			.StartAsync(message, async ctx => await self).ToPromise();
	}
}
