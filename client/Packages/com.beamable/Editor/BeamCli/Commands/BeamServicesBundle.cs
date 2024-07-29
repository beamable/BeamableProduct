
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ServicesBundleArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The beamo id of the service to bundle</summary>
        public string id;
        /// <summary>The location of the output tarball file</summary>
        public string output;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the id value was not default, then add it to the list of args.
            if ((this.id != default(string)))
            {
                genBeamCommandArgs.Add((("--id=\"" + this.id) 
                                + "\""));
            }
            // If the output value was not default, then add it to the list of args.
            if ((this.output != default(string)))
            {
                genBeamCommandArgs.Add((("--output=\"" + this.output) 
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
        public virtual ServicesBundleWrapper ServicesBundle(ServicesBundleArgs bundleArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("services");
            genBeamCommandArgs.Add("bundle");
            genBeamCommandArgs.Add(bundleArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ServicesBundleWrapper genBeamCommandWrapper = new ServicesBundleWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class ServicesBundleWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ServicesBundleWrapper OnStreamServicesGenerateTarballCommandOutput(System.Action<ReportDataPoint<BeamServicesGenerateTarballCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
