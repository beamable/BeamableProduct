
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class UnityDownloadAllNugetPackagesArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>the path to the Unity project</summary>
        public string unityProjectPath;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the unityProjectPath value to the list of args.
            genBeamCommandArgs.Add(this.unityProjectPath.ToString());
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual UnityDownloadAllNugetPackagesWrapper UnityDownloadAllNugetPackages(UnityDownloadAllNugetPackagesArgs downloadAllNugetPackagesArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("unity");
            genBeamCommandArgs.Add("download-all-nuget-packages");
            genBeamCommandArgs.Add(downloadAllNugetPackagesArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            UnityDownloadAllNugetPackagesWrapper genBeamCommandWrapper = new UnityDownloadAllNugetPackagesWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class UnityDownloadAllNugetPackagesWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual UnityDownloadAllNugetPackagesWrapper OnStreamDownloadAllNugetDepsToUnityCommandOutput(System.Action<ReportDataPoint<BeamDownloadAllNugetDepsToUnityCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
