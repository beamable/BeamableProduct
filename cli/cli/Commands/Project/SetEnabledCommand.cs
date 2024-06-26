using Beamable.Common.BeamCli.Contracts;
using cli.Dotnet;
using cli.Services;

namespace cli.Commands.Project;

public class SetEnabledCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();
	public List<string> withServiceTags = new List<string>();
	public List<string> withoutServiceTags = new List<string>();
}

public class SetEnabledCommandArgsOutput
{
	public List<SetEnabledCommandComponent> modifiedServices = new List<SetEnabledCommandComponent>();
}

public class SetEnabledCommandComponent
{
	public string service;
	public string enabled;
}

public class SetEnabledCommand : AtomicCommand<SetEnabledCommandArgs, SetEnabledCommandArgsOutput>
{
	public SetEnabledCommand() : base("enable", $"Enables a project. This modifies the {CliConstants.PROP_BEAM_ENABLED} setting in the given project files")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);
		ProjectCommand.AddServiceTagsOption(this,
			bindWithTags: (args, i) => args.withServiceTags = i,
			bindWithoutTags: (args, i) => args.withoutServiceTags = i);
	}

	public override Task<SetEnabledCommandArgsOutput> GetResult(SetEnabledCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args,
			withTags: args.withServiceTags,
			withoutTags: args.withoutServiceTags,
			includeStorage: true,
			ref args.services);

		var output = SetProjectEnabled(args.services, args.BeamoLocalSystem.BeamoManifest, true);

		return Task.FromResult(output);
	}

	public static SetEnabledCommandArgsOutput SetProjectEnabled(List<string> services, BeamoLocalManifest manifest, bool enabled)
	{
		{ // do a prepass to make sure that all services have all of their dependencies in the list

			foreach (var service in services)
			{
				if (!manifest.HttpMicroserviceLocalProtocols.TryGetValue(service, out var protocol))
				{
					continue;
				}

				var missingIds = protocol.StorageDependencyBeamIds.Where(depId => !services.Contains(depId)).ToList();
				if (missingIds.Count > 0)
				{
					throw new CliException(
						$"service cannot be included, because its dependencies are not included. service=[{service}] deps=[{string.Join(",", missingIds)}]");
				}
			}
		}


		var output = new SetEnabledCommandArgsOutput();
		foreach (var service in services)
		{
			if (!manifest.TryGetDefinition(service, out var definition))
			{
				throw new CliException($"Unknown service id=[{service}]");
			}

			var value = ProjectContextUtil.ModifyProperty(definition, CliConstants.PROP_BEAM_ENABLED, enabled ? "true" : "false");
			output.modifiedServices.Add(new SetEnabledCommandComponent
			{
				service = service,
				enabled = value
			});
		}

		return output;

	}
}


public class SetDisableCommand : AtomicCommand<SetEnabledCommandArgs, SetEnabledCommandArgsOutput>
{
	public SetDisableCommand() : base("disable", $"Disables a project. This modifies the {CliConstants.PROP_BEAM_ENABLED} setting in the given project files")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);
		ProjectCommand.AddServiceTagsOption(this,
			bindWithTags: (args, i) => args.withServiceTags = i,
			bindWithoutTags: (args, i) => args.withoutServiceTags = i);
	}

	public override Task<SetEnabledCommandArgsOutput> GetResult(SetEnabledCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args,
			withTags: args.withServiceTags,
			withoutTags: args.withoutServiceTags,
			includeStorage: true,
			ref args.services);

		var output = SetEnabledCommand.SetProjectEnabled(args.services, args.BeamoLocalSystem.BeamoManifest, false);
		return Task.FromResult(output);
	}
}

