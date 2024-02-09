
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ServicesSetLocalManifestArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Local http services paths</summary>
        public string[] services;
        /// <summary>Local storages paths</summary>
        public string[] storagePaths;
        /// <summary>Names of the services that should be disabled on remote</summary>
        public string[] disabledServices;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the services value was not default, then add it to the list of args.
            if ((this.services != default(string[])))
            {
                for (int i = 0; (i < this.services.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--services=" + this.services[i]));
                }
            }
            // If the storagePaths value was not default, then add it to the list of args.
            if ((this.storagePaths != default(string[])))
            {
                for (int i = 0; (i < this.storagePaths.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--storage-paths=" + this.storagePaths[i]));
                }
            }
            // If the disabledServices value was not default, then add it to the list of args.
            if ((this.disabledServices != default(string[])))
            {
                for (int i = 0; (i < this.disabledServices.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--disabled-services=" + this.disabledServices[i]));
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
