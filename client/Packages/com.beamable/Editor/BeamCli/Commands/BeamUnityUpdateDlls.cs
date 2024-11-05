
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class UnityUpdateDllsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The name of the service to update the dlls</summary>
        public string service;
        /// <summary>The path of the dll that will be referenced</summary>
        public string[] paths;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the service value to the list of args.
            genBeamCommandArgs.Add(this.service.ToString());
            // If the paths value was not default, then add it to the list of args.
            if ((this.paths != default(string[])))
            {
                for (int i = 0; (i < this.paths.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--paths=" + this.paths[i]));
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
        public virtual UnityUpdateDllsWrapper UnityUpdateDlls(UnityUpdateDllsArgs updateDllsArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("unity");
            genBeamCommandArgs.Add("update-dlls");
            genBeamCommandArgs.Add(updateDllsArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            UnityUpdateDllsWrapper genBeamCommandWrapper = new UnityUpdateDllsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class UnityUpdateDllsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
