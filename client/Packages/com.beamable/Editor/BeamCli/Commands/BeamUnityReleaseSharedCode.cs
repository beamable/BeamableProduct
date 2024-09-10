
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class UnityReleaseSharedCodeArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>path to csproj project</summary>
		public string csprojPath;
		/// <summary>relative path to Unity destination for src files</summary>
		public string unityPath;
		/// <summary>the name of the package to copy into</summary>
		public string packageId;
		/// <summary>relative path to Unity destination for src files</summary>
		public string packageRelativePath;
		/// <summary>relative path to src files</summary>
		public string relativeSrc;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// Add the csprojPath value to the list of args.
			genBeamCommandArgs.Add(this.csprojPath.ToString());
			// Add the unityPath value to the list of args.
			genBeamCommandArgs.Add(this.unityPath.ToString());
			// Add the packageId value to the list of args.
			genBeamCommandArgs.Add(this.packageId.ToString());
			// Add the packageRelativePath value to the list of args.
			genBeamCommandArgs.Add(this.packageRelativePath.ToString());
			// Add the relativeSrc value to the list of args.
			genBeamCommandArgs.Add(this.relativeSrc.ToString());
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual UnityReleaseSharedCodeWrapper UnityReleaseSharedCode(UnityReleaseSharedCodeArgs releaseSharedCodeArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("unity");
			genBeamCommandArgs.Add("release-shared-code");
			genBeamCommandArgs.Add(releaseSharedCodeArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			UnityReleaseSharedCodeWrapper genBeamCommandWrapper = new UnityReleaseSharedCodeWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class UnityReleaseSharedCodeWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual UnityReleaseSharedCodeWrapper OnStreamReleaseSharedUnityCodeCommandOutput(System.Action<ReportDataPoint<BeamReleaseSharedUnityCodeCommandOutput>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
