
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ProfileCheckCountersArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The path to the dotnet-counters output json file</summary>
        public string countersFilePath;
        /// <summary>The max cpu spike limit %</summary>
        public double cpuLimit;
        /// <summary>The max mem spike limit MB</summary>
        public double memLimit;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the countersFilePath value to the list of args.
            genBeamCommandArgs.Add(this.countersFilePath);
            // If the cpuLimit value was not default, then add it to the list of args.
            if ((this.cpuLimit != default(double)))
            {
                genBeamCommandArgs.Add(("--cpu-limit=" + this.cpuLimit));
            }
            // If the memLimit value was not default, then add it to the list of args.
            if ((this.memLimit != default(double)))
            {
                genBeamCommandArgs.Add(("--mem-limit=" + this.memLimit));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProfileCheckCountersWrapper ProfileCheckCounters(ProfileCheckCountersArgs checkCountersArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("profile");
            genBeamCommandArgs.Add("check-counters");
            genBeamCommandArgs.Add(checkCountersArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProfileCheckCountersWrapper genBeamCommandWrapper = new ProfileCheckCountersWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class ProfileCheckCountersWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProfileCheckCountersWrapper OnStreamCheckPerfCommandOutput(System.Action<ReportDataPoint<BeamCheckPerfCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
