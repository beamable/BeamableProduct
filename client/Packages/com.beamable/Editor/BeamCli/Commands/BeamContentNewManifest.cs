
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentNewManifestArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The manifest id to create locally</summary>
        public string manifestId;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the manifestId value to the list of args.
            genBeamCommandArgs.Add(this.manifestId.ToString());
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ContentNewManifestWrapper ContentNewManifest(ContentNewManifestArgs newManifestArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("new-manifest");
            genBeamCommandArgs.Add(newManifestArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentNewManifestWrapper genBeamCommandWrapper = new ContentNewManifestWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentNewManifestWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ContentNewManifestWrapper OnStreamContentCreateLocalManifestCommandResult(System.Action<ReportDataPoint<BeamContentCreateLocalManifestCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
