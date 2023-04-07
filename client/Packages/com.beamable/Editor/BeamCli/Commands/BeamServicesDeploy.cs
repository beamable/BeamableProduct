
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BeamCommands
    {
        /// <summary>Deploys services, either locally or remotely (to the current realm)</summary>
        /// <param name="dryrun">Should any networking happen?</param>
        /// <param name="cid">Cid to use; will default to whatever is in the file system</param>
        /// <param name="pid">Pid to use; will default to whatever is in the file system</param>
        /// <param name="host">The host endpoint for beamable</param>
        /// <param name="refreshToken">Refresh token to use for the requests</param>
        /// <param name="log">Extra logs gets printed out</param>
        /// <param name="dir">Directory to use for configuration</param>
        /// <param name="version">Show version information</param>
        /// <param name="help">Show help and usage information</param>
        /// <param name="ids">The ids for the services you wish to deploy. Ignoring this option deploys all services.If '--remote' option is set, these are the ids that'll become enabled by Beam-O once it receives the updated manifest</param>
        /// <param name="remote">(default=False) If this option is set, we publish the manifest instead</param>
        /// <param name="fromFile">(default=) If this option is set to a valid path to a ServiceManifest JSON, deploys that instead. Only works if --remote flag is set</param>
        /// <param name="comment">(default=) Requires --remote flag. Associates this comment along with the published Manifest. You'll be able to read it via the Beamable Portal</param>
        /// <param name="serviceComments">(default=System.String[]) Requires --remote flag. Any number of 'BeamoId::Comment' strings. 
        ///Associates each comment to the given Beamo Id if it's among the published services. You'll be able to read it via the Beamable Portal</param>
        public virtual Beamable.Common.BeamCli.IBeamCommand ServicesDeploy([System.Runtime.InteropServices.DefaultParameterValueAttribute(default(bool))] [System.Runtime.InteropServices.OptionalAttribute()] bool dryrun, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string cid, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string pid, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string host, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string refreshToken, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string log, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string dir, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(bool))] [System.Runtime.InteropServices.OptionalAttribute()] bool version, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(bool))] [System.Runtime.InteropServices.OptionalAttribute()] bool help, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string[]))] [System.Runtime.InteropServices.OptionalAttribute()] string[] ids, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(bool))] [System.Runtime.InteropServices.OptionalAttribute()] bool remote, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string fromFile, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string))] [System.Runtime.InteropServices.OptionalAttribute()] string comment, [System.Runtime.InteropServices.DefaultParameterValueAttribute(default(string[]))] [System.Runtime.InteropServices.OptionalAttribute()] string[] serviceComments)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Capture the path to the command
            string genBeamCommandStr = "beam services deploy";
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
            // If the ids value was not default, then add it to the list of args.
            if ((ids != default(string[])))
            {
                for (int i = 0; (i < ids.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--ids=" + ids[i]));
                }
            }
            // If the remote value was not default, then add it to the list of args.
            if ((remote != default(bool)))
            {
                genBeamCommandArgs.Add(("--remote=" + remote));
            }
            // If the fromFile value was not default, then add it to the list of args.
            if ((fromFile != default(string)))
            {
                genBeamCommandArgs.Add(("--from-file=" + fromFile));
            }
            // If the comment value was not default, then add it to the list of args.
            if ((comment != default(string)))
            {
                genBeamCommandArgs.Add(("--comment=" + comment));
            }
            // If the serviceComments value was not default, then add it to the list of args.
            if ((serviceComments != default(string[])))
            {
                for (int i = 0; (i < serviceComments.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--service-comments=" + serviceComments[i]));
                }
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
