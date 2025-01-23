
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectWriteSettingArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The BeamoId to write the settings for</summary>
        public string beamoId;
        /// <summary>If options are modified, the project needs to be re-built so that the embedded resource is available on the next execution of the service. However, this build operation may be skipped when this option is set</summary>
        public bool skipBuild;
        /// <summary>The keys of the settings. The count must match the count of the --value options</summary>
        public string[] key;
        /// <summary>The keys of the settings to be deleted. If the key is also set in a --key option, the key will not be deleted</summary>
        public string[] deleteKey;
        /// <summary>The values of the settings. The count must match the count of the --key options</summary>
        public string[] value;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the beamoId value to the list of args.
            genBeamCommandArgs.Add(this.beamoId.ToString());
            // If the skipBuild value was not default, then add it to the list of args.
            if ((this.skipBuild != default(bool)))
            {
                genBeamCommandArgs.Add(("--skip-build=" + this.skipBuild));
            }
            // If the key value was not default, then add it to the list of args.
            if ((this.key != default(string[])))
            {
                for (int i = 0; (i < this.key.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--key=" + this.key[i]));
                }
            }
            // If the deleteKey value was not default, then add it to the list of args.
            if ((this.deleteKey != default(string[])))
            {
                for (int i = 0; (i < this.deleteKey.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--delete-key=" + this.deleteKey[i]));
                }
            }
            // If the value value was not default, then add it to the list of args.
            if ((this.value != default(string[])))
            {
                for (int i = 0; (i < this.value.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--value=" + this.value[i]));
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
        public virtual ProjectWriteSettingWrapper ProjectWriteSetting(ProjectWriteSettingArgs writeSettingArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("write-setting");
            genBeamCommandArgs.Add(writeSettingArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectWriteSettingWrapper genBeamCommandWrapper = new ProjectWriteSettingWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectWriteSettingWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProjectWriteSettingWrapper OnStreamWriteProjectSettingsCommandOutput(System.Action<ReportDataPoint<BeamWriteProjectSettingsCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
