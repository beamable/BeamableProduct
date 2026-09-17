
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class WebStatusArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The local npm registry to query</summary>
        public string registry;
        /// <summary>The local unpkg-style CDN to probe</summary>
        public string cdn;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the registry value was not default, then add it to the list of args.
            if ((this.registry != default(string)))
            {
                genBeamCommandArgs.Add(("--registry=" + this.registry));
            }
            // If the cdn value was not default, then add it to the list of args.
            if ((this.cdn != default(string)))
            {
                genBeamCommandArgs.Add(("--cdn=" + this.cdn));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual WebStatusWrapper WebStatus(WebStatusArgs statusArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("web");
            genBeamCommandArgs.Add("status");
            genBeamCommandArgs.Add(statusArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            WebStatusWrapper genBeamCommandWrapper = new WebStatusWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class WebStatusWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual WebStatusWrapper OnStreamWebStatusCommandResults(System.Action<ReportDataPoint<BeamWebStatusCommandResults>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
