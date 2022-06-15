using System.CommandLine.Binding;
using Newtonsoft.Json;

namespace cli;

public interface IAppContext
{
	public bool IsDryRun { get; }
	public string Cid { get; }
	public string Pid { get; }

	/// <summary>
	/// Control how basic options are found from the console context.
	/// As we add more context variables, this method is responsible for "figuring them out"
	/// </summary>
	/// <param name="bindingContext"></param>
	void Apply(BindingContext bindingContext);
}

public class DefaultAppContext : IAppContext
{
	private readonly DryRunOption _dryRunOption;
	private readonly CidOption _cidOption;
	private readonly PasswordOption _passwordOption;
	private readonly PidOption _pidOption;
	private readonly UsernameOption _usernameOption;
	private readonly CliRequester _requester;
	public bool IsDryRun { get; set; }
	public string Cid { get; set; }
	public string Pid { get; set; }

	public DefaultAppContext(DryRunOption dryRunOption, CidOption cidOption, PidOption pidOption, CliRequester requester)
	{
		_dryRunOption = dryRunOption;
		_cidOption = cidOption;
		_pidOption = pidOption;
		_requester = requester;
	}

	public async void Apply(BindingContext bindingContext)
	{
		IsDryRun = bindingContext.ParseResult.GetValueForOption(_dryRunOption);

		var dictionary = new Dictionary<string, string>();
		bool configFileExists = false;
		if (TryToFindBeamableConfigFolder(out var path))
		{
			configFileExists = TryToReadConfigFile(path, out dictionary);
		}

		Cid = bindingContext.ParseResult.GetValueForOption(_cidOption) ??
		      (configFileExists && dictionary.ContainsKey("cid") ? dictionary["cid"] : "unset");
		Pid = bindingContext.ParseResult.GetValueForOption(_pidOption) ?? 
		      (configFileExists && dictionary.ContainsKey("pid") ? dictionary["pid"] : "unset");

		_requester.SetPidAndCid(Cid, Pid);
	}

	bool TryToFindBeamableConfigFolder(out string result)
	{
		const string CONFIG_FOLDER = ".beamable";
		result = string.Empty;
		var basePath = Directory.GetCurrentDirectory();
		if (Directory.Exists(Path.Combine(basePath, CONFIG_FOLDER)))
		{
			result = Path.Combine(basePath, CONFIG_FOLDER);
			return true;
		}

		var parentDir = Directory.GetParent(basePath);
		while (parentDir != null)
		{
			var path = Path.Combine(parentDir.FullName, CONFIG_FOLDER);
			if (Directory.Exists(path))
			{
				result = path;
				return true;
			}
			
			parentDir = parentDir.Parent;
		}
		
		return false;
	}

	bool TryToReadConfigFile(string folderPath, out Dictionary<string, string> result)
	{
		const string CONFIG_DEFAULTS = "config-defaults.txt";
		string fullPath = Path.Combine(folderPath, CONFIG_DEFAULTS);
		result = new Dictionary<string, string>();
		if (File.Exists(fullPath))
		{
			var content = File.ReadAllText(fullPath);
			result = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);

			return result is {Count: > 0};
		}

		return false;
	}
}
