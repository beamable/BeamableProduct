
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentBakeArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>published or local</summary>
        public string source;
        /// <summary>server or unity</summary>
        public string format;
        /// <summary>Manifest to bake</summary>
        public string manifestId;
        /// <summary>Directory for bakedContent.bytes and bakedManifest.bytes</summary>
        public string outputDir;
        /// <summary>fail or skip when local content differs from the published manifest</summary>
        public string localChanges;
        /// <summary>gzip or none</summary>
        public string compression;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the source value was not default, then add it to the list of args.
            if ((this.source != default(string)))
            {
                genBeamCommandArgs.Add(("--source=" + this.source));
            }
            // If the format value was not default, then add it to the list of args.
            if ((this.format != default(string)))
            {
                genBeamCommandArgs.Add(("--format=" + this.format));
            }
            // If the manifestId value was not default, then add it to the list of args.
            if ((this.manifestId != default(string)))
            {
                genBeamCommandArgs.Add(("--manifest-id=" + this.manifestId));
            }
            // If the outputDir value was not default, then add it to the list of args.
            if ((this.outputDir != default(string)))
            {
                genBeamCommandArgs.Add(("--output-dir=" + this.outputDir));
            }
            // If the localChanges value was not default, then add it to the list of args.
            if ((this.localChanges != default(string)))
            {
                genBeamCommandArgs.Add(("--local-changes=" + this.localChanges));
            }
            // If the compression value was not default, then add it to the list of args.
            if ((this.compression != default(string)))
            {
                genBeamCommandArgs.Add(("--compression=" + this.compression));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ContentBakeWrapper ContentBake(ContentBakeArgs bakeArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("bake");
            genBeamCommandArgs.Add(bakeArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentBakeWrapper genBeamCommandWrapper = new ContentBakeWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentBakeWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ContentBakeWrapper OnStreamContentBakeResult(System.Action<ReportDataPoint<BeamContentBakeResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
