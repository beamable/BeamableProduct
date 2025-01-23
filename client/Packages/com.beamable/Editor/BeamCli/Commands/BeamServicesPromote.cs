
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ServicesPromoteArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The PID for the realm from which you wish to pull the manifest from. 
        ///The current realm you are signed into will be updated to match the manifest in the given realm</summary>
        public string sourcePid;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the sourcePid value was not default, then add it to the list of args.
            if ((this.sourcePid != default(string)))
            {
                genBeamCommandArgs.Add((("--source-pid=\"" + this.sourcePid) 
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
        public virtual ServicesPromoteWrapper ServicesPromote(ServicesPromoteArgs promoteArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("services");
            genBeamCommandArgs.Add("promote");
            genBeamCommandArgs.Add(promoteArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ServicesPromoteWrapper genBeamCommandWrapper = new ServicesPromoteWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ServicesPromoteWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ServicesPromoteWrapper OnStreamManifestChecksums(System.Action<ReportDataPoint<BeamManifestChecksums>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
