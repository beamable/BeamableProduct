
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectAddPathsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Overwrite the stored extra paths for where to find projects</summary>
        public string[] saveExtraPaths;
        /// <summary>Paths to ignore when searching for services</summary>
        public string[] pathsToIgnore;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the saveExtraPaths value was not default, then add it to the list of args.
            if ((this.saveExtraPaths != default(string[])))
            {
                for (int i = 0; (i < this.saveExtraPaths.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--save-extra-paths=" + this.saveExtraPaths[i]));
                }
            }
            // If the pathsToIgnore value was not default, then add it to the list of args.
            if ((this.pathsToIgnore != default(string[])))
            {
                for (int i = 0; (i < this.pathsToIgnore.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--paths-to-ignore=" + this.pathsToIgnore[i]));
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
        public virtual ProjectAddPathsWrapper ProjectAddPaths(ProjectAddPathsArgs addPathsArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("add-paths");
            genBeamCommandArgs.Add(addPathsArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectAddPathsWrapper genBeamCommandWrapper = new ProjectAddPathsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectAddPathsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProjectAddPathsWrapper OnStreamSaveProjectPathsCommandResults(System.Action<ReportDataPoint<BeamSaveProjectPathsCommandResults>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
