
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class UnityDownloadNugetPackageArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>the nuget id of the package dep</summary>
        public string packageId;
        /// <summary>the version of the package</summary>
        public string packageVersion;
        /// <summary>the file path inside the package to copy</summary>
        public string src;
        /// <summary>the target location to place the copied files</summary>
        public string dst;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the packageId value to the list of args.
            genBeamCommandArgs.Add(this.packageId.ToString());
            // Add the packageVersion value to the list of args.
            genBeamCommandArgs.Add(this.packageVersion.ToString());
            // Add the src value to the list of args.
            genBeamCommandArgs.Add(this.src.ToString());
            // Add the dst value to the list of args.
            genBeamCommandArgs.Add(this.dst.ToString());
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual UnityDownloadNugetPackageWrapper UnityDownloadNugetPackage(UnityDownloadNugetPackageArgs downloadNugetPackageArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("unity");
            genBeamCommandArgs.Add("download-nuget-package");
            genBeamCommandArgs.Add(downloadNugetPackageArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            UnityDownloadNugetPackageWrapper genBeamCommandWrapper = new UnityDownloadNugetPackageWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class UnityDownloadNugetPackageWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual UnityDownloadNugetPackageWrapper OnStreamDownloadNugetDepToUnityCommandOutput(System.Action<ReportDataPoint<BeamDownloadNugetDepToUnityCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
