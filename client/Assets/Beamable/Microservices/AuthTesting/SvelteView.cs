using Beamable.Common;
using Beamable.Server;
using Beamable.Server.Editor;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Beamable.Microservices
{
	public class SvelteView : MicroView
	{
		// TODO: Create other standard views, like React, Vue, Solid, Angular, etc...

		public override void OnMicroserviceStarted(bool isWatch, string imageName, Type serviceType, ViewDescriptor view)
		{
			BeamableLogger.Log("Hey whaaat");
			try
			{
				var proc = new Process();
				{
					proc.StartInfo.FileName = "/usr/local/bin/npm";
					proc.StartInfo.WorkingDirectory = $"{view.SourceDirectory}/app~";
					BeamableLogger.Log("src " + proc.StartInfo.WorkingDirectory);

					proc.StartInfo.Arguments = $"run dev";
					proc.StartInfo.EnvironmentVariables["view_dist_path"] = $"{view.WorkingDir}/bundle.js";
					// Configure the process using the StartInfo properties.
					proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
					proc.EnableRaisingEvents = true;
					proc.StartInfo.RedirectStandardInput = true;
					proc.StartInfo.RedirectStandardOutput = true;
					proc.StartInfo.RedirectStandardError = true;
					proc.StartInfo.CreateNoWindow = false;
					proc.StartInfo.UseShellExecute = false;

					proc.EnableRaisingEvents = true;

					proc.OutputDataReceived += (sender, args) =>
					{
						BeamableLogger.Log("MEEEES" + " / " + args.Data);
						BeamableLogger.Log(args.Data);
					};
					proc.ErrorDataReceived += (sender, args) =>
					{
						BeamableLogger.LogError("FAIL" + " / " + args.Data);
					};

					proc.Exited += (sender, args) =>
					{
						BeamableLogger.LogError("Watch ended? " + args + " / " );
					};
					BeamableLogger.Log("Starting the thingy");
					proc.Start();
					proc.BeginOutputReadLine();
					proc.BeginErrorReadLine();

					Task.Run(() =>
					{
						proc.WaitForExit();
					});
					BeamableLogger.Log("donezo");


				}
			}
			catch (Exception ex)
			{
				BeamableLogger.Log("oh, it failed");
				BeamableLogger.LogException(ex);
			}
		}
	}
}
