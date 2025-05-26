
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class CollectorGetArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The platform for the collector executable. [osx, win, lin] or defaults to system</summary>
        public string platform;
        /// <summary>The architecture for the collector executable. [arm64, x64] or defaults to system</summary>
        public string arch;
        /// <summary>The collector version to download, or defaults to current install</summary>
        public string overrideVersion;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the platform value was not default, then add it to the list of args.
            if ((this.platform != default(string)))
            {
                genBeamCommandArgs.Add((("--platform=\"" + this.platform) 
                                + "\""));
            }
            // If the arch value was not default, then add it to the list of args.
            if ((this.arch != default(string)))
            {
                genBeamCommandArgs.Add((("--arch=\"" + this.arch) 
                                + "\""));
            }
            // If the overrideVersion value was not default, then add it to the list of args.
            if ((this.overrideVersion != default(string)))
            {
                genBeamCommandArgs.Add((("--override-version=\"" + this.overrideVersion) 
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
        public virtual CollectorGetWrapper CollectorGet(CollectorGetArgs getArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("collector");
            genBeamCommandArgs.Add("get");
            genBeamCommandArgs.Add(getArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            CollectorGetWrapper genBeamCommandWrapper = new CollectorGetWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class CollectorGetWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual CollectorGetWrapper OnStreamDownloadCollectorCommandResults(System.Action<ReportDataPoint<BeamDownloadCollectorCommandResults>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
