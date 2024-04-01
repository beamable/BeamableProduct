using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Server;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Beamable.standalone_microservice
{
	public class Program
	{
		/// <summary>
		/// The entry point for the <see cref="StandaloneMicroservice"/> service.
		/// </summary>
		public static async Task Main()
		{
			Environment.SetEnvironmentVariable("DOTNET_DiagnosticPorts", null);
			await MicroserviceBootstrapper.Prepare<StandaloneMicroservice>();
			
			// run the Microservice code
			await MicroserviceBootstrapper.Start<StandaloneMicroservice>();
		}
		
        /// <summary>
        /// This method can be called before the start of the microservice to inject some CLI information.
        /// This is only used to execute a microservice through the IDE.
        /// </summary>
        /// <param name="customArgs">Optional string with args to be used instead of the default ones.</param>
        /// <typeparam name="TMicroservice">The type of the microservice calling this method.</typeparam>
        /// <exception cref="Exception">Exception raised in case the generate-env command fails.</exception>
        public static async Task Prepare<TMicroservice>(string customArgs = null) where TMicroservice : Microservice
        {
	        var inDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
	        if (inDocker) return;
			
	        MicroserviceAttribute attribute = typeof(TMicroservice).GetCustomAttribute<MicroserviceAttribute>();
	        var serviceName = attribute.MicroserviceName;
	        
	        customArgs ??= ". --auto-deploy";
			
	        using var process = new Process();

	        
	        process.StartInfo.FileName = "beam";
	        process.StartInfo.Arguments = $"project generate-env {serviceName} {customArgs} --include-prefix=false --instance-count=10";
	        process.StartInfo.RedirectStandardOutput = true;
	        process.StartInfo.RedirectStandardError = true;
	        process.StartInfo.CreateNoWindow = true;
	        process.StartInfo.UseShellExecute = false;

	        process.Start();
	        await process.WaitForExitAsync();
			
	        var result = await process.StandardOutput.ReadToEndAsync();
	        if (process.ExitCode != 0)
	        {
		        throw new Exception($"Failed to generate-env message=[{result}]");
	        }
	        
	        var parsedOutput = JsonConvert.DeserializeObject<ReportDataPoint<GenerateEnvFileOutput>>(result);
	        if (parsedOutput.type != "stream")
	        {
		        // the output type needs to be "stream" (the default data output channel name). 
		        //  if the type isn't "stream", it is likely doing to be "error", but even if it isn't, 
		        //  it isn't the expected value.
		        throw new Exception($"Failed to parse generate-env output. raw=[{result}]");
	        }

	        // apply the environment data to the local process.
	        var envData = parsedOutput.data;
	        foreach (var envVar in envData.envVars)
	        {
		        Environment.SetEnvironmentVariable(envVar.name, envVar.value);
	        }
        }
	}
}
