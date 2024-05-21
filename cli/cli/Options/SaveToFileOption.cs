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
		
		// included for legacy reasons. 
		var saveToFileOption = new Option<bool>(saveToFileStr, 
			
			// This default value is assumed "true" in the SaveToFileOption.ShouldSaveToFile function
			getDefaultValue: () => true, 
			description: "Save login refresh token to file");
		
		// this option is legacy, and we don't really want people using it anymore. 
		saveToFileOption.IsHidden = true;
		
		var noTokenOption = new Option<bool>(noTokenSaveStr, 
			
			// This default value is assumed "false" in the SaveToFileOption.ShouldSaveToFile function
			getDefaultValue: () => false, 
			description: $"Prevent auth tokens from being saved to disk. This replaces the legacy {saveToFileStr} option.");
		
		command.AddOption<bool>(saveToFileOption, (args, value) => args.SaveToFile = value);
		command.AddOption<bool>(noTokenOption, (args, context, noTokenSave) =>
		{
			var saveToFile = context.ParseResult.GetValueForOption(saveToFileOption);
			args.SaveToFile = ShouldSaveToFile(saveToFile, noTokenSave);
		});
		
	}
	
	static bool ShouldSaveToFile(bool saveToFileOptionValue, bool noTokenSaveOptionValue)
	{
		
		switch (saveToFileOptionValue, noTokenSaveOptionValue)
		{
			case (true, true):
				// saveToFile defaults to true, but noTokenSave has higher order, and since that defaults to false, the noTokenSave wins.
				return false;
				
			case (false, true):
				// double positive is a positive. Don't save it.
				return false;
				
			case (false, false):
				// saveToFile is non-default, so it wins this case. 
				return false;
				
			case (true, false):
				// the default case is to save it!
				return true;
		}

	}
}
