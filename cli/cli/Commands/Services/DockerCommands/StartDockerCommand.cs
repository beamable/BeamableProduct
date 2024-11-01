using CliWrap;
using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace cli.DockerCommands;

public class StartDockerCommandArgs : CommandArgs
{
	public bool linksOnly;
}

public class StartDockerCommandOutput
{
	public bool attempted;
	public bool alreadyRunning;
	public bool unavailable;
	public string dockerDesktopUrl;
	public string downloadUrl;
}

public class StartDockerCommand : AtomicCommand<StartDockerCommandArgs, StartDockerCommandOutput>
{
	public StartDockerCommand() : base("start", "Start the docker daemon")
	{
	}

	public override void Configure()
	{
		AddOption(
			new Option<bool>(new string[] { "--links-only", "-l" },
				"Only return the links to download docker, but do not start"), (args, i) => args.linksOnly = i);
	}

	public override async Task<StartDockerCommandOutput> GetResult(StartDockerCommandArgs args)
	{
		var output = new StartDockerCommandOutput
		{
			downloadUrl = GetDockerDownloadLink(),
			dockerDesktopUrl = "https://www.docker.com/products/docker-desktop/"
		};
		if (args.linksOnly)
		{
			return output;
		}

		var status = await DockerStatusCommand.CheckDocker(args);


		if (!status.isCliAccessible)
		{
			output.unavailable = true;
			return output;
		}

		/*
		 * Windows: C:\Program Files\Docker\Docker 
		   Mac: /Applications/Docker.app 
		   
		 */
		if (!status.isDaemonRunning)
		{
			//https://forums.docker.com/t/start-and-stop-docker-deamon-by-windows-commandline/138122

			// start it up!
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				var command = Cli.Wrap("open").WithArguments("-a Docker");
				await command.ExecuteAsync();
				output.attempted = true;
			}
			else
			{
				throw new CliException("this command only works on mac at the moment");
			}
		}
		else
		{
			output.alreadyRunning = true;

		}

		return output;
	}

	public static string GetDockerDownloadLink()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			switch (RuntimeInformation.OSArchitecture)
			{
				case Architecture.X64:
					return "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe";
				case Architecture.Arm64:
					return "https://desktop.docker.com/win/main/arm64/Docker%20Desktop%20Installer.exe";
			}
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			switch (RuntimeInformation.OSArchitecture)
			{
				case Architecture.X64:
					return "https://desktop.docker.com/mac/main/amd64/Docker.dmg";
				case Architecture.Arm64:
					return "https://desktop.docker.com/mac/main/arm64/Docker.dmg";
			}
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return "https://docs.docker.com/desktop/linux/install/";
		}

		throw new NotImplementedException("unsupported os");
	}
}
