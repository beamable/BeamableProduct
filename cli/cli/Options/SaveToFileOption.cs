using System.CommandLine;

namespace cli;

public class SaveToFileOption
{
	/// <summary>
	/// Add the --save-to-file and --no-token-save options to a command.
	/// The --save-to-file existed in 1.x, and was defaulted to FALSE.
	///
	/// In 2.0, --no-token-save was introduced, and the intention is that tokens
	/// DO save by default. The new option exists as a way to OPT OUT. 
	/// </summary>
	/// <param name="command"></param>
	/// <typeparam name="T"></typeparam>
	public static void Bind<T>(AppCommand<T> command)
		where T : CommandArgs, IArgsWithSaveToFile
	{
		const string saveToFileStr = "--save-to-file";
		const string noTokenSaveStr = "--no-token-save";

		const bool saveToFileDefault = true;
		const bool noTokenSaveDefault = false;
		
		// included for legacy reasons. 
		var saveToFileOption = new Option<bool>(saveToFileStr, 
			
			// Hey You, Watch out! This default value affects the algorithm in the parse handling! 
			getDefaultValue: () => saveToFileDefault, 
			description: "Save login refresh token to file");
		
		// this option is legacy, and we don't really want people using it anymore. 
		saveToFileOption.IsHidden = true;
		
		var noTokenOption = new Option<bool>(noTokenSaveStr, 
			
			// Hey You, Watch out! This default value affects the algorithm in the parse handling! 
			getDefaultValue: () => noTokenSaveDefault, 
			description: $"Prevent auth tokens from being saved to disk. This replaces the legacy {saveToFileStr} option.");
		
		command.AddOption<bool>(saveToFileOption, (args, value) => args.SaveToFile = value);
		command.AddOption<bool>(noTokenOption, (args, context, noTokenSave) =>
		{
			var saveToFile = context.ParseResult.GetValueForOption(saveToFileOption);

			switch (saveToFile, noTokenSave)
			{
				case (saveToFileDefault, !noTokenSaveDefault):
					// saveToFile defaults to true, but noTokenSave has higher order, and since that defaults to false, the noTokenSave wins.
					args.SaveToFile = false;
					break;
				
				case (false, true):
					// double positive is a positive. Don't save it.
					args.SaveToFile = false;
					break;
				
				case (!saveToFileDefault, noTokenSaveDefault):
					// saveToFile is non-default, so it wins this case. 
					args.SaveToFile = false;
					break;
				
				case (saveToFileDefault, noTokenSaveDefault):
					// the default case is to save it!
					args.SaveToFile = true;
					break;
			}
			
			
		});
		
	}
}
