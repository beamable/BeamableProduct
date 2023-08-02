using Beamable.Common;
using System.Text;

namespace cli.Utils;

public class BeamCommandAssistantBuilder
{
	private readonly StringBuilder _beamCommand;

	/// <summary>
	/// Initialize with the beam command to be executed
	/// </summary>
	/// <param name="command"></param>
	/// <param name="appContext"></param>
	public BeamCommandAssistantBuilder(string command, IAppContext appContext)
	{
		_beamCommand = new StringBuilder(command);
		AddDefaultOptions(appContext);
	}

	/// <summary>
	/// Add argument to the beam command to be executed
	/// </summary>
	/// <param name="arg">The argument of the beam command</param>
	/// <returns></returns>
	public BeamCommandAssistantBuilder AddArgument(string arg)
	{
		_beamCommand.Append($" {arg}");
		return this;
	}

	private void AddDefaultOptions(IAppContext appContext)
	{
		var optionFlags = new Dictionary<string, string>()
		{
			{ "--host", appContext.Host },
			{ "--cid", appContext.Cid },
			{ "--pid", appContext.Pid },
			{ "--refresh-token", appContext.RefreshToken }
		};

		if (appContext.IsDryRun) _beamCommand.Append(" --dryrun");

		foreach ((string optionFlag, string optionValue) in optionFlags)
		{
			if (string.IsNullOrWhiteSpace(optionValue)) continue;

			_beamCommand.Append($" {optionFlag} {optionValue}");
		}
	}

	/// <summary>
	/// Add option to the beam command to be executed
	/// </summary>
	/// <param name="includeOption">Whether the option should be included</param>
	/// <param name="optionFlag">Specify the optionFlag with '--' e.g '--remote'</param>
	/// <param name="optionValue">The value of the the optionFlag</param>
	/// <returns></returns>
	public BeamCommandAssistantBuilder WithOption(bool includeOption, string optionFlag, string optionValue)
	{
		if (!includeOption) return this;

		_beamCommand.Append(string.IsNullOrWhiteSpace(optionValue) ? $" {optionFlag}" : $" {optionFlag} {optionValue}");

		return this;
	}

	/// <summary>
	/// Re-run program with the composed beam command
	/// </summary>
	public Task RunAsync()
	{
		BeamableLogger.Log($"Running 'beam {_beamCommand}'");
		Program.Main(_beamCommand.ToString().Split(' ')).Wait();
		return Task.CompletedTask;
	}
}
