using Beamable.Common;
using Spectre.Console;

namespace cli.Utils;

public static class PromiseExtensions
{
	public static async Promise<T> ShowLoading<T>(this Promise<T> self, string message="loading...", Spinner? spinner=null)
	{
		spinner ??= Spinner.Known.Default;
		return await AnsiConsole.Status()
			.Spinner(spinner)
			.StartAsync(message, async ctx => {
					try
					{
						return await self;
					}
					catch (Exception ex)
					{
						ctx.Status = "failed: " + ex.Message;
						throw ex;
					}
				}
			);
	}
}