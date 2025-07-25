
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class UnityUpdateReferencesArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The name of the service to update the references</summary>
        public string service;
        /// <summary>Relative path to the .sln file to use for the new project. If the .sln file does not exist, it will be created. When no option is configured, if this command is executing inside a .beamable folder, then the first .sln found in .beamable/.. will be used. If no .sln is found, the .sln path will be <name>.sln. If no .beamable folder exists, then the <project>/<project>.sln will be used</summary>
        public string sln;
        /// <summary>The path of the project that will be referenced</summary>
        public string[] paths;
        /// <summary>The name of the Assembly Definition</summary>
        public string[] names;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the service value to the list of args.
            genBeamCommandArgs.Add(this.service.ToString());
            // If the sln value was not default, then add it to the list of args.
            if ((this.sln != default(string)))
            {
                genBeamCommandArgs.Add(("--sln=" + this.sln));
            }
            // If the paths value was not default, then add it to the list of args.
            if ((this.paths != default(string[])))
            {
                for (int i = 0; (i < this.paths.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--paths=" + this.paths[i]));
                }
            }
            // If the names value was not default, then add it to the list of args.
            if ((this.names != default(string[])))
            {
                for (int i = 0; (i < this.names.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--names=" + this.names[i]));
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
        public virtual UnityUpdateReferencesWrapper UnityUpdateReferences(UnityUpdateReferencesArgs updateReferencesArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("unity");
            genBeamCommandArgs.Add("update-references");
            genBeamCommandArgs.Add(updateReferencesArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            UnityUpdateReferencesWrapper genBeamCommandWrapper = new UnityUpdateReferencesWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class UnityUpdateReferencesWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
