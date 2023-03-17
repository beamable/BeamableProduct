namespace cli;

public class SaveToFileOption : ConfigurableOptionFlag
{
	public SaveToFileOption() : base("save-to-file", "Save login refresh token to file") { }
}
