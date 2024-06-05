
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class UnityCopyDotnetSrcArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>path to csproj project</summary>
        public string csprojPath;
        /// <summary>relative path to Unity destination for src files</summary>
        public string unityPath;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the csprojPath value to the list of args.
            genBeamCommandArgs.Add(this.csprojPath.ToString());
            // Add the unityPath value to the list of args.
            genBeamCommandArgs.Add(this.unityPath.ToString());
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual UnityCopyDotnetSrcWrapper UnityCopyDotnetSrc(UnityCopyDotnetSrcArgs copyDotnetSrcArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("unity");
            genBeamCommandArgs.Add("copy-dotnet-src");
            genBeamCommandArgs.Add(copyDotnetSrcArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            UnityCopyDotnetSrcWrapper genBeamCommandWrapper = new UnityCopyDotnetSrcWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class UnityCopyDotnetSrcWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual UnityCopyDotnetSrcWrapper OnStreamCopyProjectSrcToUnityCommandOutput(System.Action<ReportDataPoint<BeamCopyProjectSrcToUnityCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
