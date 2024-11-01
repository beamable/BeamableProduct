
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public partial class ProjectOpenMongoArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>Name of the storage to open mongo-express to</summary>
		public Beamable.Common.Semantics.ServiceName serviceName;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the serviceName value was not default, then add it to the list of args.
			if ((this.serviceName != default(Beamable.Common.Semantics.ServiceName)))
			{
				genBeamCommandArgs.Add(this.serviceName.ToString());
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ProjectOpenMongoWrapper ProjectOpenMongo(ProjectOpenMongoArgs openMongoArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("project");
			genBeamCommandArgs.Add("open-mongo");
			genBeamCommandArgs.Add(openMongoArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ProjectOpenMongoWrapper genBeamCommandWrapper = new ProjectOpenMongoWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public partial class ProjectOpenMongoWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
	}
}
