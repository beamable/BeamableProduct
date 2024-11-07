
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public partial class ServicesDeployArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>If this option is set to a valid path to a ServiceManifest JSON, deploys that instead</summary>
		public string fromFile;
		/// <summary>Associates this comment along with the published Manifest. You'll be able to read it via the Beamable Portal</summary>
		public string comment;
		/// <summary>Any number of strings in the format BeamoId::Comment
		///Associates each comment to the given Beamo Id if it's among the published services. You'll be able to read it via the Beamable Portal</summary>
		public string[] serviceComments;
		/// <summary>A custom docker registry url to use when uploading. By default, the result from the beamo/registry network call will be used, with minor string manipulation to add https scheme, remove port specificatino, and add /v2 </summary>
		public string dockerRegistryUrl;
		/// <summary>Automatically remove service containers after they exit</summary>
		public bool keepContainers;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the fromFile value was not default, then add it to the list of args.
			if ((this.fromFile != default(string)))
			{
				genBeamCommandArgs.Add((("--from-file=\"" + this.fromFile)
								+ "\""));
			}
			// If the comment value was not default, then add it to the list of args.
			if ((this.comment != default(string)))
			{
				genBeamCommandArgs.Add((("--comment=\"" + this.comment)
								+ "\""));
			}
			// If the serviceComments value was not default, then add it to the list of args.
			if ((this.serviceComments != default(string[])))
			{
				for (int i = 0; (i < this.serviceComments.Length); i = (i + 1))
				{
					// The parameter allows multiple values
					genBeamCommandArgs.Add(("--service-comments=" + this.serviceComments[i]));
				}
			}
			// If the dockerRegistryUrl value was not default, then add it to the list of args.
			if ((this.dockerRegistryUrl != default(string)))
			{
				genBeamCommandArgs.Add((("--docker-registry-url=\"" + this.dockerRegistryUrl)
								+ "\""));
			}
			// If the keepContainers value was not default, then add it to the list of args.
			if ((this.keepContainers != default(bool)))
			{
				genBeamCommandArgs.Add(("--keep-containers=" + this.keepContainers));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ServicesDeployWrapper ServicesDeploy(ServicesDeployArgs deployArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("services");
			genBeamCommandArgs.Add("deploy");
			genBeamCommandArgs.Add(deployArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ServicesDeployWrapper genBeamCommandWrapper = new ServicesDeployWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public partial class ServicesDeployWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual ServicesDeployWrapper OnStreamServiceDeployReportResult(System.Action<ReportDataPoint<BeamServiceDeployReportResult>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
		public virtual ServicesDeployWrapper OnRemote_progressServiceRemoteDeployProgressResult(System.Action<ReportDataPoint<BeamServiceRemoteDeployProgressResult>> cb)
		{
			this.Command.On("remote_progress", cb);
			return this;
		}
	}
}
