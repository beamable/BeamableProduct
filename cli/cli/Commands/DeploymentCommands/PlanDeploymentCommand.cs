using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using Beamable.Serialization;
using cli.Commands.Project;
using cli.Deployment.Services;
using Serilog;
using Spectre.Console;
using System.CommandLine;

namespace cli.DeploymentCommands;

public interface IHasDeployPlanArgs
{
	public string Comment { get; set; }
	public string[] ServiceComments { get; set; }

	public string FromManifestFile { get; set; }
	public string ManifestId { get; set; }
	public bool UseLatestDeployedManifest { get; set; }
	public DeployMode DeployMode { get; set; }
	public bool RunHealthChecks { get; set; }
	bool UseSequentialBuild { get; set; }
}

public class PlanDeploymentCommandArgs : CommandArgs, IHasDeployPlanArgs
{
	public string toFile;
	public string Comment { get; set; }
	public string[] ServiceComments { get; set; }
	public string FromManifestFile { get; set; }
	public string ManifestId { get; set; }
	public bool UseLatestDeployedManifest { get; set; }
	public DeployMode DeployMode { get; set; }
	public bool RunHealthChecks { get; set; }
	public bool UseSequentialBuild { get; set; }
}

public class PlanReleaseProgressChannel : IResultChannel
{
	public string ChannelName => "progress";
}

public class PlanReleaseProgress
{
	public string name;
	public float ratio;
	public bool isKnownLength;
	public string serviceName;
}

public class DeploymentPlanMetadata
{
	public bool success;
	public DeployablePlan plan;
	public string planPath;
}


public static class PlanCommandExtensions {
	
	

	public static void ApplyDeployComments<TArgs>(this AppCommand<TArgs> self, DeployablePlan plan, TArgs args)
		where TArgs : CommandArgs, IHasDeployPlanArgs
	{
		{
			// clear all the comments to start, so that they don't carry over between deploys. 
			plan.manifest.comments?.Clear();
			if (plan.manifest?.manifest != null)
				foreach (var service in plan.manifest.manifest)
				{
					service.comments?.Clear();
				}
		}

		if (!string.IsNullOrEmpty(args.Comment))
		{
			plan.manifest.comments = args.Comment;
		}

		// TODO: maybe delete this?
		if (args.ServiceComments != null)
		{
			foreach (var commentGroup in args.ServiceComments)
			{
				var commentParts = commentGroup.Split("::", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (commentParts.Length != 2)
				{
					Log.Warning($"Invalid service-comment supplied. Must be in the exact form 'beamId::comment'. invalid-comment=[{commentGroup}]");
					continue;
				}

				var service = commentParts[0];
				var comment = commentParts[1];

				var foundService = plan.manifest.manifest.FirstOrDefault(x => x.serviceName == service);
				if (foundService == null)
				{
					Log.Warning($"Invalid service-comment supplied. Must reference a service in the plan. invalid-comment=[{commentGroup}]");
					continue;
				}
				
				foundService.comments = comment;
			}
		}
	}
	
	public static async Task<(DeployablePlan, string)> InteractivePlan<T, TArgs>(this T self, 
		IDependencyProvider provider, 
		TArgs args
		)
		where T : AppCommand<TArgs>
				, IResultSteam<RunProjectBuildErrorStreamChannel, RunProjectBuildErrorStream>
				, IResultSteam<PlanReleaseProgressChannel, PlanReleaseProgress> 
		where TArgs : CommandArgs, IHasDeployPlanArgs
	{
		
		var progressReporter = (IResultSteam<PlanReleaseProgressChannel, PlanReleaseProgress>)self;
		DeployablePlan plan = null;
		List<BuildImageOutput> buildResults = null;
		await AnsiConsole
			.Progress()
			.StartAsync(async ctx =>
				{
					var progressTasks = new Dictionary<string, ProgressTask>();

					(plan, buildResults) = await DeployUtil.Plan(
						provider, 
						args,
						progressHandler:
						(name, progress, isKnownLength, serviceName) =>
						{
							if (!progressTasks.TryGetValue(name, out var progressTask))
							{
								progressTasks[name] = progressTask = ctx.AddTask(name, maxValue: 1);
								if (!isKnownLength)
								{
									progressTask.IsIndeterminate = true;
								}
							}

							progressReporter.SendResults(new PlanReleaseProgress
							{
								name = name,
								isKnownLength = isKnownLength,
								ratio = progress,
								serviceName = serviceName
							});
							
							progressTask.Value = progress;
						});
					
					
				}
			);

		if (plan == null)
		{
			foreach (var report in buildResults)
			{
				if (report.success) continue;
				Log.Error($"service=[{report.service}] has build errors");
				foreach (var error in report.sourceReport.report.errors)
				{
					Log.Error($" -{error.formattedMessage}");
				}
				self.SendResults<RunProjectBuildErrorStreamChannel, RunProjectBuildErrorStream>(new RunProjectBuildErrorStream
				{
					serviceId = report.service,
					report = report.sourceReport.report
				});
			}
			
			throw new CliException("Unable to generate a plan. Please re-run with --logs v", 2, true);
		}
		else
		{
			var planPath = await DeployUtil.SavePlanToTempFolder(args.DependencyProvider, plan);
			return (plan, planPath);
		}
	}

}

public class PlanDeploymentCommand 
	: AppCommand<PlanDeploymentCommandArgs>
	, IResultSteam<DefaultStreamResultChannel, DeploymentPlanMetadata>
	, IResultSteam<RunProjectBuildErrorStreamChannel, RunProjectBuildErrorStream>
	, IResultSteam<PlanReleaseProgressChannel, PlanReleaseProgress>
{
	public override bool AutoLogOutput => false;

	public PlanDeploymentCommand() : base("plan", "Plan a deployment for later release")
	{
	}

	public override void Configure()
	{
		DeployArgs.AddPlanOptions(this);
	
		AddOption(new Option<string>(new string[] { "--to-file", "--out", "-o" }, "A file path to save the plan"),
			(args, i) => args.toFile = i);
		
	}

	public override async Task Handle(PlanDeploymentCommandArgs args)
	{
		(DeployablePlan plan, var planPath) = await this.InteractivePlan(
			args.DependencyProvider, 
			args);
		
		DeployUtil.PrintPlanInfo(plan, args, out var hasChanges);
		this.ApplyDeployComments(plan, args);
		DeployUtil.PrintPlanNextSteps(args.toFile ?? planPath, hasChanges);
		await DeployArgs.MaybeSaveToFile(args.toFile, plan);
		Log.Information("Saved plan: " + planPath);

		var results = new DeploymentPlanMetadata
		{
			success = plan != null,
			plan = plan, 
			planPath = args.toFile ?? planPath
		};
		this.SendResults<DefaultStreamResultChannel, DeploymentPlanMetadata>(results);
	}
}
