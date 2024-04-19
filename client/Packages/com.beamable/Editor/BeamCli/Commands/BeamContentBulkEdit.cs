
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class ContentBulkEditArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>Inform a subset of ','-separated manifest ids for which to return data. By default, will return all manifests</summary>
		public string[] manifestIds;
		/// <summary>An array of existing content ids</summary>
		public string[] contentIds;
		/// <summary>An array, parallel to the --content-ids, that contain the escaped properties json for each content</summary>
		public string[] contentProperties;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the manifestIds value was not default, then add it to the list of args.
			if ((this.manifestIds != default(string[])))
			{
				genBeamCommandArgs.Add((("--manifest-ids=\"" + this.manifestIds)
								+ "\""));
			}
			// If the contentIds value was not default, then add it to the list of args.
			if ((this.contentIds != default(string[])))
			{
				genBeamCommandArgs.Add((("--content-ids=\"" + this.contentIds)
								+ "\""));
			}
			// If the contentProperties value was not default, then add it to the list of args.
			if ((this.contentProperties != default(string[])))
			{
				genBeamCommandArgs.Add((("--content-properties=\"" + this.contentProperties)
								+ "\""));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ContentBulkEditWrapper ContentBulkEdit(ContentBulkEditArgs bulkEditArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("content");
			genBeamCommandArgs.Add("bulk-edit");
			genBeamCommandArgs.Add(bulkEditArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ContentBulkEditWrapper genBeamCommandWrapper = new ContentBulkEditWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class ContentBulkEditWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
	}
}
