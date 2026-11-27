
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesSourceRequestArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle name, @<namespace>/<bundle-name></summary>
        public string bundleName;
        /// <summary>The services and portal extensions whose source you want</summary>
        public System.Collections.Generic.List<string> components;
        /// <summary>The repository connection (its id) the pull request goes to</summary>
        public string repository;
        /// <summary>The release to take the source from. Defaults to the one installed on the zone, else latest</summary>
        public long version;
        /// <summary>The connection's workspace (folder) the source goes in. Defaults to its first</summary>
        public string workspace;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the bundleName value to the list of args.
            genBeamCommandArgs.Add(this.bundleName.ToString());
            // Add the components value to the list of args.
            genBeamCommandArgs.Add(this.components.ToString());
            // If the repository value was not default, then add it to the list of args.
            if ((this.repository != default(string)))
            {
                genBeamCommandArgs.Add(("--repository=" + this.repository));
            }
            // If the version value was not default, then add it to the list of args.
            if ((this.version != default(long)))
            {
                genBeamCommandArgs.Add(("--version=" + this.version));
            }
            // If the workspace value was not default, then add it to the list of args.
            if ((this.workspace != default(string)))
            {
                genBeamCommandArgs.Add(("--workspace=" + this.workspace));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesSourceRequestWrapper BundlesSourceRequest(BundlesSourceRequestArgs requestArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("source");
            genBeamCommandArgs.Add("request");
            genBeamCommandArgs.Add(requestArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesSourceRequestWrapper genBeamCommandWrapper = new BundlesSourceRequestWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesSourceRequestWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesSourceRequestWrapper OnStreamRequestSourceCommandOutput(System.Action<ReportDataPoint<BeamRequestSourceCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
