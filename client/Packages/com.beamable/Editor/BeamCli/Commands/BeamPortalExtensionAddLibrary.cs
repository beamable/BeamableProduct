
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class PortalExtensionAddLibraryArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The shared library that will be a new dependency of the specified Portal Extensions</summary>
        public string library;
        /// <summary>The list of Portal Extension names that the library will be added to (separated by whitespace)</summary>
        public string[] extensions;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the library value to the list of args.
            genBeamCommandArgs.Add(this.library.ToString());
            // If the extensions value was not default, then add it to the list of args.
            if ((this.extensions != default(string[])))
            {
                for (int i = 0; (i < this.extensions.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--extensions=" + this.extensions[i]));
                }
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual PortalExtensionAddLibraryWrapper PortalExtensionAddLibrary(PortalExtensionAddLibraryArgs addLibraryArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("portal");
            genBeamCommandArgs.Add("extension");
            genBeamCommandArgs.Add("add-library");
            genBeamCommandArgs.Add(addLibraryArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            PortalExtensionAddLibraryWrapper genBeamCommandWrapper = new PortalExtensionAddLibraryWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class PortalExtensionAddLibraryWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
