
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class VersionArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Displays the executing CLI version</summary>
        public bool showVersion;
        /// <summary>Displays the executing CLI install location</summary>
        public bool showLocation;
        /// <summary>Displays available Beamable template version</summary>
        public bool showTemplates;
        /// <summary>Displays the executing CLI install type</summary>
        public bool showType;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the showVersion value was not default, then add it to the list of args.
            if ((this.showVersion != default(bool)))
            {
                genBeamCommandArgs.Add(("--show-version=" + this.showVersion));
            }
            // If the showLocation value was not default, then add it to the list of args.
            if ((this.showLocation != default(bool)))
            {
                genBeamCommandArgs.Add(("--show-location=" + this.showLocation));
            }
            // If the showTemplates value was not default, then add it to the list of args.
            if ((this.showTemplates != default(bool)))
            {
                genBeamCommandArgs.Add(("--show-templates=" + this.showTemplates));
            }
            // If the showType value was not default, then add it to the list of args.
            if ((this.showType != default(bool)))
            {
                genBeamCommandArgs.Add(("--show-type=" + this.showType));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual VersionWrapper Version(VersionArgs versionArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("version");
            genBeamCommandArgs.Add(versionArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            VersionWrapper genBeamCommandWrapper = new VersionWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class VersionWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual VersionWrapper OnStreamVersionResults(System.Action<ReportDataPoint<BeamVersionResults>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
