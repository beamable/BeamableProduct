
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesManagementArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Do not open the portal; print its url instead</summary>
        public bool noBrowser;
        /// <summary>The portal extension in this workspace that renders the Bundles page; it is served from here, like `project run`. Pass an empty value to serve only the API</summary>
        public string extension;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the noBrowser value was not default, then add it to the list of args.
            if ((this.noBrowser != default(bool)))
            {
                genBeamCommandArgs.Add(("--no-browser=" + this.noBrowser));
            }
            // If the extension value was not default, then add it to the list of args.
            if ((this.extension != default(string)))
            {
                genBeamCommandArgs.Add(("--extension=" + this.extension));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesManagementWrapper BundlesManagement(BundlesManagementArgs managementArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("management");
            genBeamCommandArgs.Add(managementArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesManagementWrapper genBeamCommandWrapper = new BundlesManagementWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesManagementWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
