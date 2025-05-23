﻿using Beamable.Common.BeamCli;
using System.CommandLine;

namespace cli;

public class ServicesDeployCommandArgs : LoginCommandArgs
{
}

public class ServicesDeployCommand : AppCommand<ServicesDeployCommandArgs>,
	IResultSteam<DefaultStreamResultChannel, ServiceDeployReportResult>,
	IResultSteam<ServiceRemoteDeployProgressResult, ServiceRemoteDeployProgressResult>
{
	// this is a rough proxy for "obsolete". I don't want people to use this command,
	//  but if they remember it reflexively, it will at least tell them what to do.
	public override bool IsForInternalUse => true;

	public ServicesDeployCommand() :
		base("deploy",
			"Deploys services remotely to the current realm")
	{
	}

	public override void Configure()
	{
		{ // these options exist for legacy purposes. If we delete them, then the user won't see the obsolete tag, they'll see parse errors. 
			AddOption(
				new Option<string>("--from-file", () => null,
					$"If this option is set to a valid path to a ServiceManifest JSON, deploys that instead"),
				(args, i) => { });

			AddOption(
				new Option<string>("--comment", () => "",
					$"Associates this comment along with the published Manifest. You'll be able to read it via the Beamable Portal"),
				(args, i) => { });

			AddOption(new Option<string[]>("--service-comments", Array.Empty<string>,
					$"Any number of strings in the format BeamoId::Comment" +
					$"\nAssociates each comment to the given Beamo Id if it's among the published services. You'll be able to read it via the Beamable Portal")
				{
					AllowMultipleArgumentsPerToken = true
				},
				(args, i) => { });

			AddOption(new Option<string>("--docker-registry-url",
					"A custom docker registry url to use when uploading. By default, the result from the beamo/registry network call will be used, " +
					"with minor string manipulation to add https scheme, remove port specificatino, and add /v2 "),
				(args, i) => { });

			AddOption(
				new Option<bool>(new string[] { "--keep-containers", "-k" }, () => false,
					"Automatically remove service containers after they exit"),
				(args, i) => { });
		}
	}

	public override Task Handle(ServicesDeployCommandArgs args)
	{
		throw new CliException("this command is deprecated. Please use `dotnet beam deploy release` instead.");
	}
}

public class ServiceRemoteDeployProgressResult : IResultChannel
{
	public string ChannelName => "remote_progress";

	public string BeamoId;
	public double BuildAndTestProgress;
	public double ContainerUploadProgress;
}

public class ServiceDeployReportResult
{
	public bool Success;
	public string FailureReason;
}
