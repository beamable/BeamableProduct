
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ProjectShareCodeArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The .dll filepath for the built code</summary>
        public string source;
        /// <summary>A list of namespace prefixes to ignore when copying dependencies</summary>
        public string depPrefixBlacklist;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the source value to the list of args.
            genBeamCommandArgs.Add(this.source);
            // If the depPrefixBlacklist value was not default, then add it to the list of args.
            if ((this.depPrefixBlacklist != default(string)))
            {
                genBeamCommandArgs.Add(("--dep-prefix-blacklist=" + this.depPrefixBlacklist));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual Beamable.Common.BeamCli.IBeamCommand ProjectShareCode(ProjectShareCodeArgs shareCodeArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("share-code");
            genBeamCommandArgs.Add(shareCodeArgs.Serialize());
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
