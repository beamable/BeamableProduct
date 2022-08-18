namespace cli;

public class SaveToFileOption : ConfigurableOptionFlag
{
	public SaveToFileOption() : base("saveToFile", "save login refresh token to file") { }
}
