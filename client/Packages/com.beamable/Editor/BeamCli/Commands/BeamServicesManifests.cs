
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ServicesManifestsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Limits amount of manifests</summary>
        public int limit;
        /// <summary>Skip specified amount of manifests</summary>
        public int skip;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the limit value was not default, then add it to the list of args.
            if ((this.limit != default(int)))
            {
                genBeamCommandArgs.Add(("--limit=" + this.limit));
            }
            // If the skip value was not default, then add it to the list of args.
            if ((this.skip != default(int)))
            {
                genBeamCommandArgs.Add(("--skip=" + this.skip));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ServicesManifestsWrapper ServicesManifests(ServicesManifestsArgs manifestsArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("services");
            genBeamCommandArgs.Add("manifests");
            genBeamCommandArgs.Add(manifestsArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ServicesManifestsWrapper genBeamCommandWrapper = new ServicesManifestsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class ServicesManifestsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual Beamable.Common.BeamCli.BeamCommandWrapper OnStreamServiceManifestOutput(System.Action<ReportDataPoint<BeamServiceManifestOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
