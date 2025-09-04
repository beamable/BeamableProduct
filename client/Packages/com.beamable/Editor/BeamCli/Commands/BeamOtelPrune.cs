
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class OtelPruneArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>If set, will delete all telemetry files</summary>
        public bool deleteAll;
        /// <summary>Can be passed to define a custom amount of days in which data should be preserved</summary>
        public int retainingDays;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the deleteAll value was not default, then add it to the list of args.
            if ((this.deleteAll != default(bool)))
            {
                genBeamCommandArgs.Add(("--delete-all=" + this.deleteAll));
            }
            // If the retainingDays value was not default, then add it to the list of args.
            if ((this.retainingDays != default(int)))
            {
                genBeamCommandArgs.Add(("--retaining-days=" + this.retainingDays));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual OtelPruneWrapper OtelPrune(OtelPruneArgs pruneArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("otel");
            genBeamCommandArgs.Add("prune");
            genBeamCommandArgs.Add(pruneArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            OtelPruneWrapper genBeamCommandWrapper = new OtelPruneWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class OtelPruneWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
