
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ServicesSetLocalManifestArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>local http service names</summary>
        public string[] localHttpNames;
        /// <summary>local http service docker build contexts</summary>
        public string[] localHttpContexts;
        /// <summary>local http service relative docker file paths</summary>
        public string[] localHttpDockerfiles;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the localHttpNames value was not default, then add it to the list of args.
            if ((this.localHttpNames != default(string[])))
            {
                for (int i = 0; (i < this.localHttpNames.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--localHttpNames=" + this.localHttpNames[i]));
                }
            }
            // If the localHttpContexts value was not default, then add it to the list of args.
            if ((this.localHttpContexts != default(string[])))
            {
                for (int i = 0; (i < this.localHttpContexts.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--localHttpContexts=" + this.localHttpContexts[i]));
                }
            }
            // If the localHttpDockerfiles value was not default, then add it to the list of args.
            if ((this.localHttpDockerfiles != default(string[])))
            {
                for (int i = 0; (i < this.localHttpDockerfiles.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--localHttpDockerfiles=" + this.localHttpDockerfiles[i]));
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
        public virtual ServicesSetLocalManifestWrapper ServicesSetLocalManifest(ServicesSetLocalManifestArgs setLocalManifestArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("services");
            genBeamCommandArgs.Add("set-local-manifest");
            genBeamCommandArgs.Add(setLocalManifestArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ServicesSetLocalManifestWrapper genBeamCommandWrapper = new ServicesSetLocalManifestWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class ServicesSetLocalManifestWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}