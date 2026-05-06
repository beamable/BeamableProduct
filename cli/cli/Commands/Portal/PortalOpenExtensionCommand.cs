using System.CommandLine;
using cli.Services;
using cli.Utils;

namespace cli.Portal;

public class PortalOpenExtensionCommandArgs : CommandArgs
{
	public string extension;
}

public class PortalOpenExtensionCommand : AppCommand<PortalOpenExtensionCommandArgs>
{
	public PortalOpenExtensionCommand() : base("open-extension", "Open a specific portal extension page in the browser")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>(
				name: "extension",
				description: "The name of the portal extension to open"),
			(args, i) => args.extension = i);
	}

	public override async Task Handle(PortalOpenExtensionCommandArgs args)
	{
		PortalCommand.GetPortalRealmUrl(args, out var realmUrl, out var qb);

		await args.BeamoLocalSystem.InitManifest();
		var ext = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
			.FirstOrDefault(x =>
				(x.PortalExtensionDefinition?.Properties?.IsPortalExtension ?? false) &&
				x.PortalExtensionDefinition.Name == args.extension);

		if (ext == null)
			throw new CliException($"Portal extension '{args.extension}' not found in the local manifest.");

		var mountPage = BeamoLocalSystem.NormalizePortalExtensionMountPage(
			ext.PortalExtensionDefinition.Properties?.Mount?.Page);

		if (string.IsNullOrEmpty(mountPage))
			throw new CliException($"Portal extension '{args.extension}' has no mount page configured.");

		MachineHelper.OpenBrowser($"{realmUrl}/{mountPage}{qb}");
	}
}
