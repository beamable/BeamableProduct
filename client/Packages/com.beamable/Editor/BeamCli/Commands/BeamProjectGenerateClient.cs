
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectGenerateClientArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The .dll filepath for the built microservice</summary>
        public string source;
        /// <summary>The list of services to include, defaults to all local services (separated by whitespace). To use NO services, use the --exact-ids flag</summary>
        public string[] ids;
        /// <summary>By default, a blank --ids option maps to ALL available ids. When the --exact-ids flag is given, a blank --ids option maps to NO ids</summary>
        public bool exactIds;
        /// <summary>A set of BeamServiceGroup tags that will exclude the associated services. Exclusion takes precedence over inclusion</summary>
        public string[] withoutGroup;
        /// <summary>A set of BeamServiceGroup tags that will include the associated services</summary>
        public string[] withGroup;
        /// <summary>Directory to write the output client at</summary>
        public string outputDir;
        /// <summary>When true, generate the source client files to all associated projects</summary>
        public bool outputLinks;
        /// <summary>Paths to unity projects to generate clients in</summary>
        public string[] outputUnityProjects;
        /// <summary>A set of existing federation ids</summary>
        public string[] existingFedIds;
        /// <summary>A set of existing class names for federations (Obsolete)</summary>
        public string[] existingFedTypeNames;
        /// <summary>A special format, BEAMOID=PATH, that tells the generator where to place the client. The path should be relative to the linked project root (Obsolete)</summary>
        public string[] outputPathHints;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the source value to the list of args.
            genBeamCommandArgs.Add(this.source.ToString());
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
            // If the outputDir value was not default, then add it to the list of args.
            if ((this.outputDir != default(string)))
            {
                genBeamCommandArgs.Add(("--output-dir=" + this.outputDir));
            }
            // If the outputLinks value was not default, then add it to the list of args.
            if ((this.outputLinks != default(bool)))
            {
                genBeamCommandArgs.Add(("--output-links=" + this.outputLinks));
            }
            // If the outputUnityProjects value was not default, then add it to the list of args.
            if ((this.outputUnityProjects != default(string[])))
            {
                for (int i = 0; (i < this.outputUnityProjects.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--output-unity-projects=" + this.outputUnityProjects[i]));
                }
            }
            // If the existingFedIds value was not default, then add it to the list of args.
            if ((this.existingFedIds != default(string[])))
            {
                for (int i = 0; (i < this.existingFedIds.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--existing-fed-ids=" + this.existingFedIds[i]));
                }
            }
            // If the existingFedTypeNames value was not default, then add it to the list of args.
            if ((this.existingFedTypeNames != default(string[])))
            {
                for (int i = 0; (i < this.existingFedTypeNames.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--existing-fed-type-names=" + this.existingFedTypeNames[i]));
                }
            }
            // If the outputPathHints value was not default, then add it to the list of args.
            if ((this.outputPathHints != default(string[])))
            {
                for (int i = 0; (i < this.outputPathHints.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--output-path-hints=" + this.outputPathHints[i]));
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
        public virtual ProjectGenerateClientWrapper ProjectGenerateClient(ProjectGenerateClientArgs generateClientArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("generate-client");
            genBeamCommandArgs.Add(generateClientArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectGenerateClientWrapper genBeamCommandWrapper = new ProjectGenerateClientWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectGenerateClientWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProjectGenerateClientWrapper OnStreamGenerateClientFileEvent(System.Action<ReportDataPoint<BeamGenerateClientFileEvent>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
