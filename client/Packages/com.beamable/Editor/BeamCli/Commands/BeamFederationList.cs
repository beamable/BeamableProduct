
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class FederationListArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>Filter the services by the types of federations they provide</summary>
		public string type;
		/// <summary>Filter the services by the service name</summary>
		public string name;
		/// <summary>Filter the services by the federation namespace</summary>
		public string @namespace;
		/// <summary>Filter the services by the playerId of the author</summary>
		public long player;
		/// <summary>After piping the current list of services, keeps on listening and pipe them again every change</summary>
		public bool listen;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the type value was not default, then add it to the list of args.
			if ((this.type != default(string)))
			{
				genBeamCommandArgs.Add((("--type=\"" + this.type)
								+ "\""));
			}
			// If the name value was not default, then add it to the list of args.
			if ((this.name != default(string)))
			{
				genBeamCommandArgs.Add((("--name=\"" + this.name)
								+ "\""));
			}
			// If the namespace value was not default, then add it to the list of args.
			if ((this.@namespace != default(string)))
			{
				genBeamCommandArgs.Add((("--namespace=\"" + this.@namespace)
								+ "\""));
			}
			// If the player value was not default, then add it to the list of args.
			if ((this.player != default(long)))
			{
				genBeamCommandArgs.Add(("--player=" + this.player));
			}
			// If the listen value was not default, then add it to the list of args.
			if ((this.listen != default(bool)))
			{
				genBeamCommandArgs.Add(("--listen=" + this.listen));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual FederationListWrapper FederationList(FederationListArgs listArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("federation");
			genBeamCommandArgs.Add("list");
			genBeamCommandArgs.Add(listArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			FederationListWrapper genBeamCommandWrapper = new FederationListWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class FederationListWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual FederationListWrapper OnStreamListServicesCommandOutput(System.Action<ReportDataPoint<BeamListServicesCommandOutput>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
