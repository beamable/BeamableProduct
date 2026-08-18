
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class WebUseArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Directory tree to scan for portal extensions; defaults to the working directory</summary>
        public string workspace;
        /// <summary>The local npm registry holding the build</summary>
        public string registry;
        /// <summary>The version to pin; defaults to the newest local build, from the registry's 'local' dist-tag</summary>
        public string version;
        /// <summary>Rewrite the pins without running npm install</summary>
        public bool skipInstall;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the workspace value was not default, then add it to the list of args.
            if ((this.workspace != default(string)))
            {
                genBeamCommandArgs.Add(("--workspace=" + this.workspace));
            }
            // If the registry value was not default, then add it to the list of args.
            if ((this.registry != default(string)))
            {
                genBeamCommandArgs.Add(("--registry=" + this.registry));
            }
            // If the version value was not default, then add it to the list of args.
            if ((this.version != default(string)))
            {
                genBeamCommandArgs.Add(("--version=" + this.version));
            }
            // If the skipInstall value was not default, then add it to the list of args.
            if ((this.skipInstall != default(bool)))
            {
                genBeamCommandArgs.Add(("--skip-install=" + this.skipInstall));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual WebUseWrapper WebUse(WebUseArgs useArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("web");
            genBeamCommandArgs.Add("use");
            genBeamCommandArgs.Add(useArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            WebUseWrapper genBeamCommandWrapper = new WebUseWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class WebUseWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual WebUseWrapper OnStreamWebUseCommandResults(System.Action<ReportDataPoint<BeamWebUseCommandResults>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
