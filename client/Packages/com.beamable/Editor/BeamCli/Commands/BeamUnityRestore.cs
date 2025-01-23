
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class UnityRestoreArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The path to the dotnet csproj path</summary>
        public string csproj;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the csproj value was not default, then add it to the list of args.
            if ((this.csproj != default(string)))
            {
                genBeamCommandArgs.Add((("--csproj=\"" + this.csproj) 
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
        public virtual UnityRestoreWrapper UnityRestore(UnityRestoreArgs restoreArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("unity");
            genBeamCommandArgs.Add("restore");
            genBeamCommandArgs.Add(restoreArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            UnityRestoreWrapper genBeamCommandWrapper = new UnityRestoreWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class UnityRestoreWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual UnityRestoreWrapper OnStreamRestoreProjectCommandOutput(System.Action<ReportDataPoint<BeamRestoreProjectCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
