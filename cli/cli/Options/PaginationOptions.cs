using Beamable.Common.Content;
using System.CommandLine;

namespace cli.Options;

public static class PaginationOptions
{
	public struct PaginationValue
	{
		public int limit, offset;
	}

	public interface IPaginationArgs
	{
		public OptionalInt Limit { get; set; }
		public OptionalInt Offset { get; set; }
	}
	
	public static void AddPaginationOptions<TArgs>(
		AppCommand<TArgs> command,
		int defaultLimit=-1,
		int defaultOffset=-1)
		where TArgs : CommandArgs, IPaginationArgs
	{
		var limitOption = new Option<int>("--limit", () => defaultLimit, "The limit of resources. A value of -1 means no limit");
		var offsetOption = new Option<int>("--offset", () => defaultOffset, "The offset of resources. A value of -1 means no offset");

		command.AddOption(limitOption, (args, context, limit) =>
		{
			var offset = context.ParseResult.GetValueForOption(offsetOption);

			args.Limit = new OptionalInt();
			if (limit > 0)
			{
				args.Limit.Set(limit);
			}
			args.Offset = new OptionalInt();
			if (offset > 0)
			{
				args.Offset.Set(offset);
			}
		});
	}
}
