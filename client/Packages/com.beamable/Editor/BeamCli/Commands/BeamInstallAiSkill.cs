
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class InstallAiSkillArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Install to .claude/skills/</summary>
        public bool claude;
        /// <summary>Install to .cursor/skills/</summary>
        public bool cursor;
        /// <summary>Install to .windsurf/skills/</summary>
        public bool windsurf;
        /// <summary>Install to .opencode/skills/</summary>
        public bool opencode;
        /// <summary>Overwrite existing skill files even if they have been customized</summary>
        public bool force;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the claude value was not default, then add it to the list of args.
            if ((this.claude != default(bool)))
            {
                genBeamCommandArgs.Add(("--claude=" + this.claude));
            }
            // If the cursor value was not default, then add it to the list of args.
            if ((this.cursor != default(bool)))
            {
                genBeamCommandArgs.Add(("--cursor=" + this.cursor));
            }
            // If the windsurf value was not default, then add it to the list of args.
            if ((this.windsurf != default(bool)))
            {
                genBeamCommandArgs.Add(("--windsurf=" + this.windsurf));
            }
            // If the opencode value was not default, then add it to the list of args.
            if ((this.opencode != default(bool)))
            {
                genBeamCommandArgs.Add(("--opencode=" + this.opencode));
            }
            // If the force value was not default, then add it to the list of args.
            if ((this.force != default(bool)))
            {
                genBeamCommandArgs.Add(("--force=" + this.force));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual InstallAiSkillWrapper InstallAiSkill(InstallAiSkillArgs installAiSkillArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("install-ai-skill");
            genBeamCommandArgs.Add(installAiSkillArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            InstallAiSkillWrapper genBeamCommandWrapper = new InstallAiSkillWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class InstallAiSkillWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual InstallAiSkillWrapper OnStreamInstallAISkillsCommandResult(System.Action<ReportDataPoint<BeamInstallAISkillsCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
