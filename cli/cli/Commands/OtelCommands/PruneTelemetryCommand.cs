using Beamable.Common;
using beamable.otel.exporter.Utils;
using Beamable.Server;
using cli.Utils;
using System.CommandLine;
using System.Diagnostics;

namespace cli.OtelCommands;

[Serializable]
public class PruneTelemetryCommandArgs : CommandArgs
{
	public bool ClearEverything;
	public int MaxDaysToStore;
	public string ProcessId;
}

public class PruneTelemetryCommand : AppCommand<PruneTelemetryCommandArgs>, IEmptyResult
{
	private const int DefaultRetainingDays = 10;

	public PruneTelemetryCommand() : base("prune", "Clears telemetry data that was exported to files by normal usage of the CLI")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<bool>("--delete-all", () => false,"If set, will delete all telemetry files"),
			(args, i) => args.ClearEverything = i);

		AddOption(new Option<int>("--retaining-days", () => DefaultRetainingDays, "Can be passed to define a custom amount of days in which data should be preserved"),
			(args, i) => args.MaxDaysToStore = i);
		
		AddOption(new Option<string>("--process-id", () => string.Empty, "Defines the process Id that called this method. If is not passed a new process ID will be generated"), 
			(args, id) => args.ProcessId = id);
	}

	public override async Task Handle(PruneTelemetryCommandArgs args)
	{
		int processId = !string.IsNullOrEmpty(args.ProcessId) && int.TryParse(args.ProcessId, out int processIdInt)? processIdInt : Environment.ProcessId;
		bool couldLockFile = await args.ProcessFileLocker.LockFile(OtelCommand.OTEL_COMMANDS_LOCK_FILE, processId);
		if (!couldLockFile)
		{
			Log.Information($"Couldn't run operation because lock file {OtelCommand.OTEL_COMMANDS_LOCK_FILE} is locked by another process");
		}
		
		try
		{
			if (string.IsNullOrEmpty(args.ConfigService.ConfigTempOtelLogsDirectoryPath))
			{
				throw new CliException("Couldn't resolve telemetry logs path");
			}

			if (string.IsNullOrEmpty(args.ConfigService.ConfigTempOtelTracesDirectoryPath))
			{
				throw new CliException("Couldn't resolve telemetry traces path");
			}

			if (string.IsNullOrEmpty(args.ConfigService.ConfigTempOtelMetricsDirectoryPath))
			{
				throw new CliException("Couldn't resolve telemetry metrics path");
			}

			var result = new CleanupResult();
			result.Success = true;

			BeamableLogger.Log($"Clearing logs on path: {args.ConfigService.ConfigTempOtelLogsDirectoryPath}");
			var logsResult = FolderManagementHelper.ClearOldTelemetryFiles(args.ConfigService.ConfigTempOtelLogsDirectoryPath,
				args.MaxDaysToStore, args.ClearEverything);
			result.Merge(logsResult);
			result.Success = result.Success && logsResult.Success;

			BeamableLogger.Log($"Clearing traces on path: {args.ConfigService.ConfigTempOtelTracesDirectoryPath}");
			var tracesResult = FolderManagementHelper.ClearOldTelemetryFiles(args.ConfigService.ConfigTempOtelTracesDirectoryPath,
				args.MaxDaysToStore, args.ClearEverything);
			result.Merge(tracesResult);
			result.Success = result.Success && tracesResult.Success;

			BeamableLogger.Log($"Clearing metrics on path: {args.ConfigService.ConfigTempOtelMetricsDirectoryPath}");
			var metricsResult = FolderManagementHelper.ClearOldTelemetryFiles(args.ConfigService.ConfigTempOtelMetricsDirectoryPath,
				args.MaxDaysToStore, args.ClearEverything);
			result.Merge(metricsResult);
			result.Success = result.Success && metricsResult.Success;

			if (!result.Success)
			{
				throw new CliException("Failed to clear all files. Got the following list of errors: \n" + string.Join("\n", result.ErrorMessages));
			}
			else
			{
				BeamableLogger.Log("Telemetry data cleared successfully!");
				BeamableLogger.Log($"Total data cleared: {DirectoryUtils.FormatBytes(result.BytesFreed)}");
			}
		}
		finally
		{
			await args.ProcessFileLocker.UnlockFile(OtelCommand.OTEL_COMMANDS_LOCK_FILE, processId);
		}
	}
}
