
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class UnityVerifyPackageMetasArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>the path to the Unity package folder to verify</summary>
        public string packagePath;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the packagePath value to the list of args.
            genBeamCommandArgs.Add(this.packagePath.ToString());
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual UnityVerifyPackageMetasWrapper UnityVerifyPackageMetas(UnityVerifyPackageMetasArgs verifyPackageMetasArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("unity");
            genBeamCommandArgs.Add("verify-package-metas");
            genBeamCommandArgs.Add(verifyPackageMetasArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            UnityVerifyPackageMetasWrapper genBeamCommandWrapper = new UnityVerifyPackageMetasWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class UnityVerifyPackageMetasWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual UnityVerifyPackageMetasWrapper OnStreamVerifyPackageMetasCommandOutput(System.Action<ReportDataPoint<BeamVerifyPackageMetasCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
