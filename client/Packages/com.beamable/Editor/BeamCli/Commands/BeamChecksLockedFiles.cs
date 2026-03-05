
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ChecksLockedFilesArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The folder path to check for locked files</summary>
        public string folderPath;
        /// <summary>The file pattern to check for</summary>
        public string pattern;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the folderPath value was not default, then add it to the list of args.
            if ((this.folderPath != default(string)))
            {
                genBeamCommandArgs.Add(("--folder-path=" + this.folderPath));
            }
            // If the pattern value was not default, then add it to the list of args.
            if ((this.pattern != default(string)))
            {
                genBeamCommandArgs.Add(("--pattern=" + this.pattern));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ChecksLockedFilesWrapper ChecksLockedFiles(ChecksLockedFilesArgs lockedFilesArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("checks");
            genBeamCommandArgs.Add("locked-files");
            genBeamCommandArgs.Add(lockedFilesArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ChecksLockedFilesWrapper genBeamCommandWrapper = new ChecksLockedFilesWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ChecksLockedFilesWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ChecksLockedFilesWrapper OnStreamLockedFilesCheckCommandResult(System.Action<ReportDataPoint<BeamLockedFilesCheckCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
