using System.CommandLine;
using System.CommandLine.Help;

namespace cli.Utils;

public class BeamHelpBuilder
{
	private const string Indent = "  ";
	public int MaxWidth { get; } = 120;
	public LocalizationResources LocalizationResources { get; }

	public BeamHelpBuilder(HelpContext context)
	{
		MaxWidth = context.HelpBuilder.MaxWidth;
		LocalizationResources = context.HelpBuilder.LocalizationResources;
	}

	/// <summary>
	/// This is mostly a rip of HelpBuilder.Default.SubcommandsSection,
	/// except that some of the utilities in that function cannot be overriden, so I
	/// couldn't think of a way to extend it without just copying it. Text is cheap.
	/// </summary>
	/// <param name="context"></param>
	public void WriteSubcommands(HelpContext context)
	{
		var subcommands = context.Command.Subcommands.Where(x =>
		{

			var allOption = context.ParseResult.GetValueForOption(AllHelpOption.Instance);

			if (x is IAppCommand appCommand && appCommand.IsForInternalUse)
			{
				return allOption;
			}

			return !x.IsHidden;
		}).Select(x => context.HelpBuilder.GetTwoColumnRow(x, context)).ToArray();

		if (subcommands.Length <= 0)
		{
			return;
		}

		WriteHeading(LocalizationResources.HelpCommandsTitle(), null, context.Output);
		context.HelpBuilder.WriteColumns(subcommands, context);
	}



	private void WriteHeading(string heading, string description, TextWriter writer)
	{
		if (!string.IsNullOrWhiteSpace(heading))
		{
			writer.WriteLine(heading);
		}

		if (!string.IsNullOrWhiteSpace(description))
		{
			int maxWidth = MaxWidth - Indent.Length;
			foreach (var part in WrapText(description!, maxWidth))
			{
				writer.Write(Indent);
				writer.WriteLine(part);
			}
		}
	}
	/// <summary>
	/// Writes the specified help rows, aligning output in columns.
	/// </summary>
	/// <param name="items">The help items to write out in columns.</param>
	/// <param name="context">The help context.</param>
	public void WriteColumns(IReadOnlyList<TwoColumnHelpRow> items, HelpContext context)
	{
		if (items.Count == 0)
		{
			return;
		}

		int windowWidth = MaxWidth;

		int firstColumnWidth = items.Select(x => x.FirstColumnText.Length).Max();
		int secondColumnWidth = items.Select(x => x.SecondColumnText.Length).Max();

		if (firstColumnWidth + secondColumnWidth + Indent.Length + Indent.Length > windowWidth)
		{
			int firstColumnMaxWidth = windowWidth / 2 - Indent.Length;
			if (firstColumnWidth > firstColumnMaxWidth)
			{
				firstColumnWidth = items.SelectMany(x => WrapText(x.FirstColumnText, firstColumnMaxWidth).Select(x => x.Length)).Max();
			}
			secondColumnWidth = windowWidth - firstColumnWidth - Indent.Length - Indent.Length;
		}

		for (var i = 0; i < items.Count; i++)
		{
			var helpItem = items[i];
			IEnumerable<string> firstColumnParts = WrapText(helpItem.FirstColumnText, firstColumnWidth);
			IEnumerable<string> secondColumnParts = WrapText(helpItem.SecondColumnText, secondColumnWidth);

			foreach (var (first, second) in ZipWithEmpty(firstColumnParts, secondColumnParts))
			{
				context.Output.Write($"{Indent}{first}");
				if (!string.IsNullOrWhiteSpace(second))
				{
					int padSize = firstColumnWidth - first.Length;
					string padding = "";
					if (padSize > 0)
					{
						padding = new string(' ', padSize);
					}

					context.Output.Write($"{padding}{Indent}{second}");
				}

				context.Output.WriteLine();
			}
		}

		static IEnumerable<(string, string)> ZipWithEmpty(IEnumerable<string> first, IEnumerable<string> second)
		{
			using var enum1 = first.GetEnumerator();
			using var enum2 = second.GetEnumerator();
			bool hasFirst = false, hasSecond = false;
			while ((hasFirst = enum1.MoveNext()) | (hasSecond = enum2.MoveNext()))
			{
				yield return (hasFirst ? enum1.Current : "", hasSecond ? enum2.Current : "");
			}
		}
	}

	private static IEnumerable<string> WrapText(string text, int maxWidth)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			yield break;
		}

		//First handle existing new lines
		var parts = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

		foreach (string part in parts)
		{
			if (part.Length <= maxWidth)
			{
				yield return part;
			}
			else
			{
				//Long item, wrap it based on the width
				for (int i = 0; i < part.Length;)
				{
					if (part.Length - i < maxWidth)
					{
						yield return part.Substring(i);
						break;
					}
					else
					{
						int length = -1;
						for (int j = 0; j + i < part.Length && j < maxWidth; j++)
						{
							if (char.IsWhiteSpace(part[i + j]))
							{
								length = j + 1;
							}
						}
						if (length == -1)
						{
							length = maxWidth;
						}
						yield return part.Substring(i, length);

						i += length;
					}
				}
			}
		}
	}
}
