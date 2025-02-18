
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ChecksScanArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>automatically fix known issues for the given code prefixes, or * to fix everything </summary>
        public string[] fix;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the fix value was not default, then add it to the list of args.
            if ((this.fix != default(string[])))
            {
                for (int i = 0; (i < this.fix.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--fix=" + this.fix[i]));
                }
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ChecksScanWrapper ChecksScan(ChecksScanArgs scanArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("checks");
            genBeamCommandArgs.Add("scan");
            genBeamCommandArgs.Add(scanArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ChecksScanWrapper genBeamCommandWrapper = new ChecksScanWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ChecksScanWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ChecksScanWrapper OnStreamCheckResultsForBeamoId(System.Action<ReportDataPoint<BeamCheckResultsForBeamoId>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
        public virtual ChecksScanWrapper OnFixedCheckFixedResult(System.Action<ReportDataPoint<BeamCheckFixedResult>> cb)
        {
            this.Command.On("fixed", cb);
            return this;
        }
    }
}
