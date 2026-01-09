
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentArchiveManifestArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The manifest id to archive</summary>
        public string manifestId;
        /// <summary>When true, restore an archived manifest</summary>
        public bool unarchive;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the manifestId value to the list of args.
            genBeamCommandArgs.Add(this.manifestId.ToString());
            // If the unarchive value was not default, then add it to the list of args.
            if ((this.unarchive != default(bool)))
            {
                genBeamCommandArgs.Add(("--unarchive=" + this.unarchive));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ContentArchiveManifestWrapper ContentArchiveManifest(ContentArchiveManifestArgs archiveManifestArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("archive-manifest");
            genBeamCommandArgs.Add(archiveManifestArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentArchiveManifestWrapper genBeamCommandWrapper = new ContentArchiveManifestWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentArchiveManifestWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ContentArchiveManifestWrapper OnStreamContentArchiveManifestCommandResult(System.Action<ReportDataPoint<BeamContentArchiveManifestCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
