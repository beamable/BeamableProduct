
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BeamCommands
    {
        /// <summary>Download the Beamable Open API specs</summary>
        /// <param name="dryrun">Should any networking happen?</param>
        /// <param name="cid">Cid to use; will default to whatever is in the file system</param>
        /// <param name="pid">Pid to use; will default to whatever is in the file system</param>
        /// <param name="host">The host endpoint for beamable</param>
        /// <param name="refreshToken">Refresh token to use for the requests</param>
        /// <param name="log">Extra logs gets printed out</param>
        /// <param name="dir">Directory to use for configuration</param>
        /// <param name="version">Show version information</param>
        /// <param name="help">Show help and usage information</param>
        /// <param name="output">(default=) When null or empty, the generated code will be sent to standard-out. When there is a output value, the file or files will be written to the path</param>
        /// <param name="filter">(default=) Filter which open apis to generate. An empty string matches everything</param>
        public virtual Beamable.Common.BeamCli.IBeamCommand OapiDownload([System.Runtime.InteropServices.DefaultParameterValueAttribute(default(bool))] [System.Runtime.InteropServices.OptionalAttribute()] bool dryrun, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string cid, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string pid, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string host, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string refreshToken, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string log, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string dir, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(bool))] [System.Runtime.InteropServices.OptionalAttribute()] bool version, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(bool))] [System.Runtime.InteropServices.OptionalAttribute()] bool help, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string output, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string filter)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Capture the path to the command
            string genBeamCommandStr = "beam oapi download";
            // The first argument is always the path to the command itself
            genBeamCommandArgs.Add(genBeamCommandStr);
            // If the dryrun value was not default, then add it to the list of args.
            if ((dryrun != default(bool)))
            {
                genBeamCommandArgs.Add(("--dryrun=" + dryrun));
            }
            // If the cid value was not default, then add it to the list of args.
            if ((cid != default(string)))
            {
                genBeamCommandArgs.Add(("--cid=" + cid));
            }
            // If the pid value was not default, then add it to the list of args.
            if ((pid != default(string)))
            {
                genBeamCommandArgs.Add(("--pid=" + pid));
            }
            // If the host value was not default, then add it to the list of args.
            if ((host != default(string)))
            {
                genBeamCommandArgs.Add(("--host=" + host));
            }
            // If the refreshToken value was not default, then add it to the list of args.
            if ((refreshToken != default(string)))
            {
                genBeamCommandArgs.Add(("--refresh-token=" + refreshToken));
            }
            // If the log value was not default, then add it to the list of args.
            if ((log != default(string)))
            {
                genBeamCommandArgs.Add(("--log=" + log));
            }
            // If the dir value was not default, then add it to the list of args.
            if ((dir != default(string)))
            {
                genBeamCommandArgs.Add(("--dir=" + dir));
            }
            // If the version value was not default, then add it to the list of args.
            if ((version != default(bool)))
            {
                genBeamCommandArgs.Add(("--version=" + version));
            }
            // If the help value was not default, then add it to the list of args.
            if ((help != default(bool)))
            {
                genBeamCommandArgs.Add(("--help=" + help));
            }
            // If the output value was not default, then add it to the list of args.
            if ((output != default(string)))
            {
                genBeamCommandArgs.Add(("--output=" + output));
            }
            // If the filter value was not default, then add it to the list of args.
            if ((filter != default(string)))
            {
                genBeamCommandArgs.Add(("--filter=" + filter));
            }
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            // Return the command!
            return command;
        }
    }
}
