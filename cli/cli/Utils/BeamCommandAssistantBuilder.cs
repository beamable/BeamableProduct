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
		int indexOfFirstDefaultFlag = _beamCommand.ToString().IndexOf("--", StringComparison.Ordinal);

		if (indexOfFirstDefaultFlag > 0) _beamCommand.Insert(indexOfFirstDefaultFlag, $"{arg} ");
		else _beamCommand.Append($" {arg}");

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
	/// <param name="optionValues">The value(s) of the the optionFlag</param>
	/// <returns></returns>
	public BeamCommandAssistantBuilder WithOption(bool includeOption, string optionFlag, params string[] optionValues)
	{
		if (!includeOption) return this;

		_beamCommand.Append(optionValues.Length == 0
			? $" {optionFlag}"
			: $" {optionFlag}{string.Join(string.Empty, optionValues.Select(optionValue => $" {optionValue}"))}");

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
