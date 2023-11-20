using Spectre.Console;

namespace cli.Utils;

public static class AnsiConsoleHelper
{
	private static readonly Style BeamSelectionHightlightStyle =
		new Style(Color.NavajoWhite1, new Color(130, 108, 207), Decoration.Bold);

	public static SelectionPrompt<T> AddBeamHightlight<T>(this SelectionPrompt<T> obj)
		where T : notnull =>
		obj.HighlightStyle(BeamSelectionHightlightStyle);

	public static MultiSelectionPrompt<T> AddBeamHightlight<T>(this MultiSelectionPrompt<T> obj)
		where T : notnull =>
		obj.HighlightStyle(BeamSelectionHightlightStyle);
}
