using System;
using System.Collections;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2
{
	public partial class UsamWindow2
	{
		bool clearDockerPendingActions = false;
		void CheckDocker(string verb, Action afterDocker, out bool cancelled)
		{
			cancelled = false;
			clearDockerPendingActions = true;
			// wait until there is a valid docker session...
			if (usam.latestDockerStatus == null)
			{
				// EditorUtility.DisplayProgressBar(
				// 	title: "Checking for Docker...",
				// 	info: "Beamable requires Docker for this action, but has not been able to " +
				// 	      "confirm Docker is installed and running", 
				// 	progress: .5f);

				// return false;
				usam.receivedAnyDockerStateYet.Then(_ => afterDocker?.Invoke());
				return;
			}

			var cli = usam.latestDockerStatus.isCliAccessible;
			var daemon = usam.latestDockerStatus.isDaemonRunning;

			var hasDockerButItIsNotRunning = cli && !daemon;
			var doesNotHaveDocker = !cli;

			if (doesNotHaveDocker)
			{
				var shouldDownload = EditorUtility.DisplayDialog(
					title: "Install Docker",
					message: $"Beamable requires Docker to {verb}. We could not find " +
							 "Docker installed on your machine. Would you like to go the installation webpage " +
							 "and automatically start downloading Docker? When it has finished downloading, you " +
							 $"must run the installer. After you have installed Docker, try to {verb} again. ",
					ok: "Start Downloading Docker",
					cancel: "Cancel");

				if (shouldDownload)
				{
					usam.OpenDockerInstallPage();
				}
				else
				{
					cancelled = true;
				}
			}
			else if (hasDockerButItIsNotRunning)
			{
				var shouldStart = EditorUtility.DisplayDialog(
					title: "Start Docker",
					message: $"Beamable requires Docker to {verb}. Docker appears to be installed " +
							 "on your machine, but it is not running. Would you like to start Docker? ",
					ok: "Start Docker",
					cancel: "Cancel");
				if (shouldStart)
				{
					usam.StartDocker(running =>
					{
						if (running)
						{
							clearDockerPendingActions = false;
							// need to wait until our own notification system thinks its running...
							//  otherwise you may get stuck at the "welcome to docker" nonsense
							//  on the first boot of Docker Desktop

							usam._dispatcher.Run("enqueue-docker-run", DelayRun());

						}
						else
						{
							EditorUtility.DisplayDialog(
								title: "Unable to start Docker",
								message: "We weren't able to start Docker. Please start it manually. ",
								ok: "Okay");
						}
					});
				}
				else
				{
					cancelled = true;
				}
			}
			else
			{
				afterDocker?.Invoke();
			}
			return;

			IEnumerator DelayRun()
			{
				while (!clearDockerPendingActions && !usam.latestDockerStatus.isDaemonRunning)
				{
					yield return new WaitForSecondsRealtime(.5f);
					if (usam.latestDockerStatus.isDaemonRunning)
					{
						afterDocker?.Invoke();
					}
				}
			}
		}
	}
}
