
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class LocalUpArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Path to the manifest (defaults to .beamable/local-stack.json)</summary>
        public string config;
        /// <summary>Override the manifest backend host</summary>
        public string host;
        /// <summary>Override the manifest portal URL</summary>
        public string portalUrl;
        /// <summary>Run only these steps (comma/space separated names)</summary>
        public string only;
        /// <summary>Skip these steps (comma/space separated names)</summary>
        public string skip;
        /// <summary>Run the stack detached: services keep running after `up` returns; manage with `beam local ps`/`logs`/`stop`. Default runs attached — logs stream live and the stack stops when `up` exits (Ctrl+C)</summary>
        public bool runDetached;
        /// <summary>Build the C# gateway, Scala services, and portal deps before launching (microservices/extensions always build via project run)</summary>
        public bool build;
        /// <summary>Persist per-run logs under the workspace (.beamable/local-stack-logs/run-<id>); without it logs go to a temp folder and are removed on `beam local stop`</summary>
        public bool saveLogs;
        /// <summary>Do not auto-create a local realm when the saved login is invalid — just warn (by default `up` creates one after a docker cleanup)</summary>
        public bool noCreateRealm;
        /// <summary>Customer name to use when creating the local realm</summary>
        public string realmCustomer;
        /// <summary>Project name to use when creating the local realm</summary>
        public string realmProject;
        /// <summary>Account email to use when creating the local realm</summary>
        public string realmEmail;
        /// <summary>Alias to use when creating the local realm</summary>
        public string realmAlias;
        /// <summary>Account password to use when creating the local realm</summary>
        public string realmPassword;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the config value was not default, then add it to the list of args.
            if ((this.config != default(string)))
            {
                genBeamCommandArgs.Add(("--config=" + this.config));
            }
            // If the host value was not default, then add it to the list of args.
            if ((this.host != default(string)))
            {
                genBeamCommandArgs.Add(("--host=" + this.host));
            }
            // If the portalUrl value was not default, then add it to the list of args.
            if ((this.portalUrl != default(string)))
            {
                genBeamCommandArgs.Add(("--portal-url=" + this.portalUrl));
            }
            // If the only value was not default, then add it to the list of args.
            if ((this.only != default(string)))
            {
                genBeamCommandArgs.Add(("--only=" + this.only));
            }
            // If the skip value was not default, then add it to the list of args.
            if ((this.skip != default(string)))
            {
                genBeamCommandArgs.Add(("--skip=" + this.skip));
            }
            // If the runDetached value was not default, then add it to the list of args.
            if ((this.runDetached != default(bool)))
            {
                genBeamCommandArgs.Add(("--run-detached=" + this.runDetached));
            }
            // If the build value was not default, then add it to the list of args.
            if ((this.build != default(bool)))
            {
                genBeamCommandArgs.Add(("--build=" + this.build));
            }
            // If the saveLogs value was not default, then add it to the list of args.
            if ((this.saveLogs != default(bool)))
            {
                genBeamCommandArgs.Add(("--save-logs=" + this.saveLogs));
            }
            // If the noCreateRealm value was not default, then add it to the list of args.
            if ((this.noCreateRealm != default(bool)))
            {
                genBeamCommandArgs.Add(("--no-create-realm=" + this.noCreateRealm));
            }
            // If the realmCustomer value was not default, then add it to the list of args.
            if ((this.realmCustomer != default(string)))
            {
                genBeamCommandArgs.Add(("--realm-customer=" + this.realmCustomer));
            }
            // If the realmProject value was not default, then add it to the list of args.
            if ((this.realmProject != default(string)))
            {
                genBeamCommandArgs.Add(("--realm-project=" + this.realmProject));
            }
            // If the realmEmail value was not default, then add it to the list of args.
            if ((this.realmEmail != default(string)))
            {
                genBeamCommandArgs.Add(("--realm-email=" + this.realmEmail));
            }
            // If the realmAlias value was not default, then add it to the list of args.
            if ((this.realmAlias != default(string)))
            {
                genBeamCommandArgs.Add(("--realm-alias=" + this.realmAlias));
            }
            // If the realmPassword value was not default, then add it to the list of args.
            if ((this.realmPassword != default(string)))
            {
                genBeamCommandArgs.Add(("--realm-password=" + this.realmPassword));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual LocalUpWrapper LocalUp(LocalUpArgs upArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("local");
            genBeamCommandArgs.Add("up");
            genBeamCommandArgs.Add(upArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            LocalUpWrapper genBeamCommandWrapper = new LocalUpWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class LocalUpWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual LocalUpWrapper OnStreamLocalStackUpResultStream(System.Action<ReportDataPoint<BeamLocalStackUpResultStream>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
