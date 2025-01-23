
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class VersionConstructArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The major semantic version number</summary>
        public int major;
        /// <summary>The minor semantic version number</summary>
        public int minor;
        /// <summary>The patch semantic version number</summary>
        public int patch;
        /// <summary>When true, the command will return a non zero exit code if the specified version already exists on Nuget</summary>
        public bool validate;
        /// <summary>Sets the version string to a nightly version number, and will include the date string automatically</summary>
        public bool nightly;
        /// <summary>Sets the version string to a release candidate version number</summary>
        public int rc;
        /// <summary>Sets the version string to an experimental version number</summary>
        public int exp;
        /// <summary>Sets the version string to a production version number</summary>
        public bool prod;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the major value to the list of args.
            genBeamCommandArgs.Add(this.major.ToString());
            // Add the minor value to the list of args.
            genBeamCommandArgs.Add(this.minor.ToString());
            // Add the patch value to the list of args.
            genBeamCommandArgs.Add(this.patch.ToString());
            // If the validate value was not default, then add it to the list of args.
            if ((this.validate != default(bool)))
            {
                genBeamCommandArgs.Add(("--validate=" + this.validate));
            }
            // If the nightly value was not default, then add it to the list of args.
            if ((this.nightly != default(bool)))
            {
                genBeamCommandArgs.Add(("--nightly=" + this.nightly));
            }
            // If the rc value was not default, then add it to the list of args.
            if ((this.rc != default(int)))
            {
                genBeamCommandArgs.Add(("--rc=" + this.rc));
            }
            // If the exp value was not default, then add it to the list of args.
            if ((this.exp != default(int)))
            {
                genBeamCommandArgs.Add(("--exp=" + this.exp));
            }
            // If the prod value was not default, then add it to the list of args.
            if ((this.prod != default(bool)))
            {
                genBeamCommandArgs.Add(("--prod=" + this.prod));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual VersionConstructWrapper VersionConstruct(VersionConstructArgs constructArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("version");
            genBeamCommandArgs.Add("construct");
            genBeamCommandArgs.Add(constructArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            VersionConstructWrapper genBeamCommandWrapper = new VersionConstructWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class VersionConstructWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual VersionConstructWrapper OnStreamConstructVersionOutput(System.Action<ReportDataPoint<BeamConstructVersionOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
