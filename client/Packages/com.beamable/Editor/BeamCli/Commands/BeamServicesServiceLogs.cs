
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ServicesServiceLogsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The Unique Id for this service within this Beamable CLI context</summary>
        public string id;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the id value was not default, then add it to the list of args.
            if ((this.id != default(string)))
            {
                genBeamCommandArgs.Add(("--id=" + this.id));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ServicesServiceLogsWrapper ServicesServiceLogs(ServicesServiceLogsArgs serviceLogsArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("services");
            genBeamCommandArgs.Add("service-logs");
            genBeamCommandArgs.Add(serviceLogsArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ServicesServiceLogsWrapper genBeamCommandWrapper = new ServicesServiceLogsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class ServicesServiceLogsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ServicesServiceLogsWrapper OnStreamGetSignedUrlResponse(System.Action<ReportDataPoint<BeamGetSignedUrlResponse>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
