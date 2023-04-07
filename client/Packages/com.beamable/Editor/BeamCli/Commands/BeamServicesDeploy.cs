
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ServicesDeployArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The ids for the services you wish to deploy. Ignoring this option deploys all services.If '--remote' option is set, these are the ids that'll become enabled by Beam-O once it receives the updated manifest</summary>
        public string[] ids;
        /// <summary>If this option is set, we publish the manifest instead</summary>
        public bool remote;
        /// <summary>If this option is set to a valid path to a ServiceManifest JSON, deploys that instead. Only works if --remote flag is set</summary>
        public string fromFile;
        /// <summary>Requires --remote flag. Associates this comment along with the published Manifest. You'll be able to read it via the Beamable Portal</summary>
        public string comment;
        /// <summary>Requires --remote flag. Any number of 'BeamoId::Comment' strings. 
        ///Associates each comment to the given Beamo Id if it's among the published services. You'll be able to read it via the Beamable Portal</summary>
        public string[] serviceComments;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the ids value was not default, then add it to the list of args.
            if ((this.ids != default(string[])))
            {
                for (int i = 0; (i < this.ids.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--ids=" + this.ids[i]));
                }
            }
            // If the remote value was not default, then add it to the list of args.
            if ((this.remote != default(bool)))
            {
                genBeamCommandArgs.Add(("--remote=" + this.remote));
            }
            // If the fromFile value was not default, then add it to the list of args.
            if ((this.fromFile != default(string)))
            {
                genBeamCommandArgs.Add(("--from-file=" + this.fromFile));
            }
            // If the comment value was not default, then add it to the list of args.
            if ((this.comment != default(string)))
            {
                genBeamCommandArgs.Add(("--comment=" + this.comment));
            }
            // If the serviceComments value was not default, then add it to the list of args.
            if ((this.serviceComments != default(string[])))
            {
                for (int i = 0; (i < this.serviceComments.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--service-comments=" + this.serviceComments[i]));
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
        public virtual Beamable.Common.BeamCli.IBeamCommand ServicesDeploy(ServicesDeployArgs deployArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("services");
            genBeamCommandArgs.Add("deploy");
            genBeamCommandArgs.Add(deployArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            // Return the command!
            return command;
        }
    }
}
