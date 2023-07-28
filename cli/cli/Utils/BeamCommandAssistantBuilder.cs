using Beamable.Common;
using System.Diagnostics;

namespace cli.Utils;

public class BeamCommandAssistantBuilder
{
	private string _beamCommand;

	/// <summary>
	/// Initialize with the beam command to be executed
	/// </summary>
	/// <param name="command"></param>
	public BeamCommandAssistantBuilder(string command)
	{
		_beamCommand = command;
	}

	/// <summary>
	/// Add argument to the beam command to be executed
	/// </summary>
	/// <param name="arg">The argument of the beam command</param>
	/// <returns></returns>
	public BeamCommandAssistantBuilder AddArgument(string arg)
	{
		_beamCommand += $" {arg}";
		return this;
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

        _beamCommand += string.IsNullOrWhiteSpace(optionValue) ? $" {optionFlag}" : $" {optionFlag} {optionValue}";

		return this;
	}

	/// <summary>
	/// Execute the composed beam command
	/// </summary>
	public void ExecuteAsync()
	{
		BeamableLogger.Log($"Running 'beam {_beamCommand}'");
		using var process = new Process();
		process.StartInfo.FileName = "beam";
		process.StartInfo.Arguments = _beamCommand;
		process.Start();
		process.WaitForExit();
	}
}
