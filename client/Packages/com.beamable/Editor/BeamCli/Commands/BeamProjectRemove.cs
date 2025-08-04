
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectRemoveArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Relative path to the .sln file to use for the new project. If the .sln file does not exist, it will be created. When no option is configured, if this command is executing inside a .beamable folder, then the first .sln found in .beamable/.. will be used. If no .sln is found, the .sln path will be <name>.sln. If no .beamable folder exists, then the <project>/<project>.sln will be used</summary>
        public string sln;
        /// <summary>Relative path to directory where project should be created. Defaults to "SOLUTION_DIR/services"</summary>
        public string serviceDirectory;
        /// <summary>The list of services to include, defaults to all local services (separated by whitespace). To use NO services, use the --exact-ids flag</summary>
        public string[] ids;
        /// <summary>By default, a blank --ids option maps to ALL available ids. When the --exact-ids flag is given, a blank --ids option maps to NO ids</summary>
        public bool exactIds;
        /// <summary>A set of BeamServiceGroup tags that will exclude the associated services. Exclusion takes precedence over inclusion</summary>
        public string[] withoutGroup;
        /// <summary>A set of BeamServiceGroup tags that will include the associated services</summary>
        public string[] withGroup;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
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
            // If the ids value was not default, then add it to the list of args.
            if ((this.ids != default(string[])))
            {
                for (int i = 0; (i < this.ids.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--ids=" + this.ids[i]));
                }
            }
            // If the exactIds value was not default, then add it to the list of args.
            if ((this.exactIds != default(bool)))
            {
                genBeamCommandArgs.Add(("--exact-ids=" + this.exactIds));
            }
            // If the withoutGroup value was not default, then add it to the list of args.
            if ((this.withoutGroup != default(string[])))
            {
                for (int i = 0; (i < this.withoutGroup.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--without-group=" + this.withoutGroup[i]));
                }
            }
            // If the withGroup value was not default, then add it to the list of args.
            if ((this.withGroup != default(string[])))
            {
                for (int i = 0; (i < this.withGroup.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--with-group=" + this.withGroup[i]));
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
        public virtual ProjectRemoveWrapper ProjectRemove(ProjectRemoveArgs removeArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("remove");
            genBeamCommandArgs.Add(removeArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectRemoveWrapper genBeamCommandWrapper = new ProjectRemoveWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectRemoveWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProjectRemoveWrapper OnStreamDeleteProjectCommandOutput(System.Action<ReportDataPoint<BeamDeleteProjectCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
