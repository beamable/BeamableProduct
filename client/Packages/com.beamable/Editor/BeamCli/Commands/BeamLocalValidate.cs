
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class LocalValidateArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Toolchain directory to inspect (default: ~/.beamable-toolchain, or $BEAM_TOOLCHAIN_DIR)</summary>
        public string toolchainDir;
        /// <summary>Path to the local-stack manifest to validate against (defaults to .beamable/local-stack.json)</summary>
        public string config;
        /// <summary>Also run the AWS preflight (credentials, assume-role, the JWT signing secret, buckets, scheduler queue)</summary>
        public bool withAws;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the toolchainDir value was not default, then add it to the list of args.
            if ((this.toolchainDir != default(string)))
            {
                genBeamCommandArgs.Add(("--toolchain-dir=" + this.toolchainDir));
            }
            // If the config value was not default, then add it to the list of args.
            if ((this.config != default(string)))
            {
                genBeamCommandArgs.Add(("--config=" + this.config));
            }
            // If the withAws value was not default, then add it to the list of args.
            if ((this.withAws != default(bool)))
            {
                genBeamCommandArgs.Add(("--with-aws=" + this.withAws));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual LocalValidateWrapper LocalValidate(LocalValidateArgs validateArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("local");
            genBeamCommandArgs.Add("validate");
            genBeamCommandArgs.Add(validateArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            LocalValidateWrapper genBeamCommandWrapper = new LocalValidateWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class LocalValidateWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual LocalValidateWrapper OnStreamLocalStackValidateCommandResult(System.Action<ReportDataPoint<BeamLocalStackValidateCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
