
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class WebResetArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Absolute path to the BeamableProduct checkout holding portal-localdev/; defaults to searching upwards from the working directory</summary>
        public string productDir;
        /// <summary>Leave the package manager caches alone; by default the cached @beamable tarballs are evicted</summary>
        public bool keepCaches;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the productDir value was not default, then add it to the list of args.
            if ((this.productDir != default(string)))
            {
                genBeamCommandArgs.Add(("--product-dir=" + this.productDir));
            }
            // If the keepCaches value was not default, then add it to the list of args.
            if ((this.keepCaches != default(bool)))
            {
                genBeamCommandArgs.Add(("--keep-caches=" + this.keepCaches));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual WebResetWrapper WebReset(WebResetArgs resetArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("web");
            genBeamCommandArgs.Add("reset");
            genBeamCommandArgs.Add(resetArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            WebResetWrapper genBeamCommandWrapper = new WebResetWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class WebResetWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual WebResetWrapper OnStreamWebResetCommandResults(System.Action<ReportDataPoint<BeamWebResetCommandResults>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
