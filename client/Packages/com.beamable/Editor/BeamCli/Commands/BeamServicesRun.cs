
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ServicesRunArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>[DEPRECATED] Force the services to run with amd64 CPU architecture, useful when deploying from computers with ARM architecture</summary>
        public bool forceAmdCpuArch;
        /// <summary>The ids for the services you wish to deploy. Ignoring this option deploys all services</summary>
        public string[] ids;
        /// <summary>By default, this command forces the services to run with amd64 CPU architecture, which is the architecture used in Docker</summary>
        public bool forceLocalCpu;
        /// <summary>Automatically remove service containers after they exit</summary>
        public bool keepContainers;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the forceAmdCpuArch value was not default, then add it to the list of args.
            if ((this.forceAmdCpuArch != default(bool)))
            {
                genBeamCommandArgs.Add(("--force-amd-cpu-arch=" + this.forceAmdCpuArch));
            }
            // If the ids value was not default, then add it to the list of args.
            if ((this.ids != default(string[])))
            {
                for (int i = 0; (i < this.ids.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--ids=" + this.ids[i]));
                }
            }
            // If the forceLocalCpu value was not default, then add it to the list of args.
            if ((this.forceLocalCpu != default(bool)))
            {
                genBeamCommandArgs.Add(("--force-local-cpu=" + this.forceLocalCpu));
            }
            // If the keepContainers value was not default, then add it to the list of args.
            if ((this.keepContainers != default(bool)))
            {
                genBeamCommandArgs.Add(("--keep-containers=" + this.keepContainers));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ServicesRunWrapper ServicesRun(ServicesRunArgs runArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("services");
            genBeamCommandArgs.Add("run");
            genBeamCommandArgs.Add(runArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ServicesRunWrapper genBeamCommandWrapper = new ServicesRunWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ServicesRunWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ServicesRunWrapper OnStreamServiceRunReportResult(System.Action<ReportDataPoint<BeamServiceRunReportResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
        public virtual ServicesRunWrapper OnLocal_progressServiceRunProgressResult(System.Action<ReportDataPoint<BeamServiceRunProgressResult>> cb)
        {
            this.Command.On("local_progress", cb);
            return this;
        }
    }
}
