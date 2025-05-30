using Serilog;
using System.CommandLine;
using System.Diagnostics;

namespace cli;

public class ConfigurableOption : Option<String>
{
	public string OptionName { get; }

	public ConfigurableOption(string optionName, string desc)
		: base($"--{optionName}", desc)
	{
		OptionName = optionName;
	}
}

public class ConfigurableIntOption : Option<int>
{
	public string OptionName { get; }

	public ConfigurableIntOption(string optionName, string desc)
		: base($"--{optionName}", desc)
	{
		OptionName = optionName;
	}
}

public class RequireProcessIdOption : Option<int>
{
	public string OptionName { get; }

	public RequireProcessIdOption() : base($"--require-process-id", $"Listens to the given process id. Terminates this long-running command when the it no longer is running")
	{
		OptionName = "require-process-id";
	}

	public static void ConfigureRequiredProcessIdWatcher(int requireProcessId)
	{
		if (requireProcessId <= 0) return;

		var _ = Task.Run(async () =>
		{
			try
			{
				Log.Debug($"Running process-watcher loop for required process id=[{requireProcessId}]");
				var processExists = true;
				do
				{
					await Task.Delay(TimeSpan.FromSeconds(1));
					try
					{
						var p = Process.GetProcessById(requireProcessId);
						if (p.HasExited)
						{
							processExists = false;
						}
					}
					catch
					{
						processExists = false;
					}
				} while (processExists);

				// terminate. 
				Log.Information("Quitting because required process no longer exists");
				Environment.Exit(0);
			}
			catch (Exception ex)
			{
				Log.Error($"Error while watching for required process id. type=[{ex.GetType().Name}] message=[{ex.Message}]");
			}
		});
	}
}

public class ConfigurableOptionFlag : Option<bool>
{
	public string OptionName { get; }

	public ConfigurableOptionFlag(string optionName, string desc)
		: base($"--{optionName}", desc)
	{
		OptionName = optionName;
	}
}

public class ConfigurableOptionList : Option<IEnumerable<string>>
{
	public string OptionName { get; }

	public ConfigurableOptionList(string optionName, string desc)
		: base($"--{optionName}", desc)
	{
		OptionName = optionName;
		AllowMultipleArgumentsPerToken = true;
	}
}
