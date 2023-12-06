
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class ServicesSetLocalManifestArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>Local http service names</summary>
		public string[] localHttpNames;
		/// <summary>Local http service docker build contexts</summary>
		public string[] localHttpContexts;
		/// <summary>Local http service relative docker file paths</summary>
		public string[] localHttpDockerFiles;
		/// <summary>Local http service required storage, use format <service-name>:<storage-name></summary>
		public string[] storageDependencies;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the localHttpNames value was not default, then add it to the list of args.
			if ((this.localHttpNames != default(string[])))
			{
				for (int i = 0; (i < this.localHttpNames.Length); i = (i + 1))
				{
					// The parameter allows multiple values
					genBeamCommandArgs.Add(("--local-http-names=" + this.localHttpNames[i]));
				}
			}
			// If the localHttpContexts value was not default, then add it to the list of args.
			if ((this.localHttpContexts != default(string[])))
			{
				for (int i = 0; (i < this.localHttpContexts.Length); i = (i + 1))
				{
					// The parameter allows multiple values
					genBeamCommandArgs.Add(("--local-http-contexts=" + this.localHttpContexts[i]));
				}
			}
			// If the localHttpDockerFiles value was not default, then add it to the list of args.
			if ((this.localHttpDockerFiles != default(string[])))
			{
				for (int i = 0; (i < this.localHttpDockerFiles.Length); i = (i + 1))
				{
					// The parameter allows multiple values
					genBeamCommandArgs.Add(("--local-http-docker-files=" + this.localHttpDockerFiles[i]));
				}
			}
			// If the storageDependencies value was not default, then add it to the list of args.
			if ((this.storageDependencies != default(string[])))
			{
				for (int i = 0; (i < this.storageDependencies.Length); i = (i + 1))
				{
					// The parameter allows multiple values
					genBeamCommandArgs.Add(("--storage-dependencies=" + this.storageDependencies[i]));
				}
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ServicesSetLocalManifestWrapper ServicesSetLocalManifest(ServicesSetLocalManifestArgs setLocalManifestArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("services");
			genBeamCommandArgs.Add("set-local-manifest");
			genBeamCommandArgs.Add(setLocalManifestArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ServicesSetLocalManifestWrapper genBeamCommandWrapper = new ServicesSetLocalManifestWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class ServicesSetLocalManifestWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
	}
}
