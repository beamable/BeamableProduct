
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentSaveArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest</summary>
        public string[] manifestIds;
        /// <summary>An array of existing content ids</summary>
        public string[] contentIds;
        /// <summary>An array, parallel to the --content-ids, that contain the escaped properties json for each content</summary>
        public string[] contentProperties;
        /// <summary>When this is set, this will ignore your local state and save the properties directly to disk</summary>
        public bool force;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the manifestIds value was not default, then add it to the list of args.
            if ((this.manifestIds != default(string[])))
            {
                genBeamCommandArgs.Add(("--manifest-ids=" + this.manifestIds));
            }
            // If the contentIds value was not default, then add it to the list of args.
            if ((this.contentIds != default(string[])))
            {
                genBeamCommandArgs.Add(("--content-ids=" + this.contentIds));
            }
            // If the contentProperties value was not default, then add it to the list of args.
            if ((this.contentProperties != default(string[])))
            {
                genBeamCommandArgs.Add(("--content-properties=" + this.contentProperties));
            }
            // If the force value was not default, then add it to the list of args.
            if ((this.force != default(bool)))
            {
                genBeamCommandArgs.Add(("--force=" + this.force));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ContentSaveWrapper ContentSave(ContentSaveArgs saveArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("save");
            genBeamCommandArgs.Add(saveArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentSaveWrapper genBeamCommandWrapper = new ContentSaveWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentSaveWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
