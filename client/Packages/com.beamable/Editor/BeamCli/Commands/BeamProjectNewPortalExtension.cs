
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectNewPortalExtensionArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Name of the new project</summary>
        public Beamable.Common.Semantics.ServiceName name;
        /// <summary>Relative path to the .sln file to use for the new project. If the .sln file does not exist, it will be created. When no option is configured, if this command is executing inside a .beamable folder, then the first .sln found in .beamable/.. will be used. If no .sln is found, the .sln path will be <name>.sln. If no .beamable folder exists, then the <project>/<project>.sln will be used</summary>
        public string sln;
        /// <summary>Relative path to directory where project should be created. Defaults to "SOLUTION_DIR/services"</summary>
        public string serviceDirectory;
        /// <summary>Specify the page that the portal extension should added</summary>
        public string mountPage;
        /// <summary>Specify the place on the page that the portal extension should added</summary>
        public string mountSelector;
        /// <summary>Specify the navigation group of the extension. This is only valid when the extension is a full page</summary>
        public string mountGroup;
        /// <summary>Specify the navigation label of the extension. This is only valid when the extension is a full page</summary>
        public string mountLabel;
        /// <summary>Specify the Material Design Icon (mdi) that will be used for the extension's navigation. This is only valid when the extension is a full page</summary>
        public string mountIcon;
        /// <summary>Specify the order of the mount group</summary>
        public int mountGroupOrder;
        /// <summary>Specify the order of the mount label</summary>
        public int mountLabelOrder;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the name value to the list of args.
            genBeamCommandArgs.Add(this.name.ToString());
            // If the sln value was not default, then add it to the list of args.
            if ((this.sln != default(string)))
            {
                genBeamCommandArgs.Add(("--sln=" + this.sln));
            }
            // If the serviceDirectory value was not default, then add it to the list of args.
            if ((this.serviceDirectory != default(string)))
            {
                genBeamCommandArgs.Add(("--service-directory=" + this.serviceDirectory));
            }
            // If the mountPage value was not default, then add it to the list of args.
            if ((this.mountPage != default(string)))
            {
                genBeamCommandArgs.Add(("--mount-page=" + this.mountPage));
            }
            // If the mountSelector value was not default, then add it to the list of args.
            if ((this.mountSelector != default(string)))
            {
                genBeamCommandArgs.Add(("--mount-selector=" + this.mountSelector));
            }
            // If the mountGroup value was not default, then add it to the list of args.
            if ((this.mountGroup != default(string)))
            {
                genBeamCommandArgs.Add(("--mount-group=" + this.mountGroup));
            }
            // If the mountLabel value was not default, then add it to the list of args.
            if ((this.mountLabel != default(string)))
            {
                genBeamCommandArgs.Add(("--mount-label=" + this.mountLabel));
            }
            // If the mountIcon value was not default, then add it to the list of args.
            if ((this.mountIcon != default(string)))
            {
                genBeamCommandArgs.Add(("--mount-icon=" + this.mountIcon));
            }
            // If the mountGroupOrder value was not default, then add it to the list of args.
            if ((this.mountGroupOrder != default(int)))
            {
                genBeamCommandArgs.Add(("--mount-group-order=" + this.mountGroupOrder));
            }
            // If the mountLabelOrder value was not default, then add it to the list of args.
            if ((this.mountLabelOrder != default(int)))
            {
                genBeamCommandArgs.Add(("--mount-label-order=" + this.mountLabelOrder));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectNewPortalExtensionWrapper ProjectNewPortalExtension(ProjectNewPortalExtensionArgs portalExtensionArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("new");
            genBeamCommandArgs.Add("portal-extension");
            genBeamCommandArgs.Add(portalExtensionArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectNewPortalExtensionWrapper genBeamCommandWrapper = new ProjectNewPortalExtensionWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectNewPortalExtensionWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
