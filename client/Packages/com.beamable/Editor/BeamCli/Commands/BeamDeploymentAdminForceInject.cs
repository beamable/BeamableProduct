
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class DeploymentAdminForceInjectArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle name</summary>
        public string bundleName;
        /// <summary>The bundle's namespace (a customer alias). Defaults to the current context's customer alias; pass it to force-inject a bundle owned by a different customer</summary>
        public string @namespace;
        /// <summary>The target customer id to force the bundle onto. Defaults to the current project's cid; pass it to force-inject onto a different customer</summary>
        public string cid;
        /// <summary>The target realm id to force the bundle onto. When omitted, the bundle is forced CID-wide as the default for every realm</summary>
        public string realm;
        /// <summary>The bundle content checksum to force (sha256:<checksum>). Defaults to the bundle's latest published checksum when omitted</summary>
        public string checksum;
        /// <summary>Remove the force-injection override instead of setting it</summary>
        public bool unset;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the bundleName value to the list of args.
            genBeamCommandArgs.Add(this.bundleName.ToString());
            // If the namespace value was not default, then add it to the list of args.
            if ((this.@namespace != default(string)))
            {
                genBeamCommandArgs.Add(("--namespace=" + this.@namespace));
            }
            // If the cid value was not default, then add it to the list of args.
            if ((this.cid != default(string)))
            {
                genBeamCommandArgs.Add(("--cid=" + this.cid));
            }
            // If the realm value was not default, then add it to the list of args.
            if ((this.realm != default(string)))
            {
                genBeamCommandArgs.Add(("--realm=" + this.realm));
            }
            // If the checksum value was not default, then add it to the list of args.
            if ((this.checksum != default(string)))
            {
                genBeamCommandArgs.Add(("--checksum=" + this.checksum));
            }
            // If the unset value was not default, then add it to the list of args.
            if ((this.unset != default(bool)))
            {
                genBeamCommandArgs.Add(("--unset=" + this.unset));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual DeploymentAdminForceInjectWrapper DeploymentAdminForceInject(DeploymentAdminForceInjectArgs forceInjectArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("deployment");
            genBeamCommandArgs.Add("admin");
            genBeamCommandArgs.Add("force-inject");
            genBeamCommandArgs.Add(forceInjectArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            DeploymentAdminForceInjectWrapper genBeamCommandWrapper = new DeploymentAdminForceInjectWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class DeploymentAdminForceInjectWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual DeploymentAdminForceInjectWrapper OnStreamForceInjectBundleCommandOutput(System.Action<ReportDataPoint<BeamForceInjectBundleCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
