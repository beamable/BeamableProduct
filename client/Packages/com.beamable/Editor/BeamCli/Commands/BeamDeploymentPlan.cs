
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class DeploymentPlanArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Associates this comment along with the published Manifest. You'll be able to read it via the Beamable Portal</summary>
        public string comment;
        /// <summary>Any number of strings in the format BeamoId::Comment
        ///Associates each comment to the given Beamo Id if it's among the published services. You'll be able to read it via the Beamable Portal</summary>
        public string[] serviceComments;
        /// <summary>A manifest json file to use to create a plan</summary>
        public string fromManifest;
        /// <summary>A manifest id to download and use to create a plan</summary>
        public string fromManifestId;
        /// <summary>Run health checks on services</summary>
        public bool runHealthChecks;
        /// <summary>Restart existing deployed services</summary>
        public bool redeploy;
        /// <summary>Build services sequentially instead of all together</summary>
        public bool buildSequentially;
        /// <summary>Create a Release that merges your current local environment to the existing remote services. Existing deployed services will not be removed</summary>
        public bool merge;
        /// <summary>Create a Release that completely overrides the existing remote services. Existing deployed services that are not present locally will be removed (default)</summary>
        public bool replace;
        /// <summary>A file path to save the plan</summary>
        public string toFile;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the comment value was not default, then add it to the list of args.
            if ((this.comment != default(string)))
            {
                genBeamCommandArgs.Add((("--comment=\"" + this.comment) 
                                + "\""));
            }
            // If the serviceComments value was not default, then add it to the list of args.
            if ((this.serviceComments != default(string[])))
            {
                for (int i = 0; (i < this.serviceComments.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--service-comments=" + this.serviceComments[i]));
                }
            }
            // If the fromManifest value was not default, then add it to the list of args.
            if ((this.fromManifest != default(string)))
            {
                genBeamCommandArgs.Add((("--from-manifest=\"" + this.fromManifest) 
                                + "\""));
            }
            // If the fromManifestId value was not default, then add it to the list of args.
            if ((this.fromManifestId != default(string)))
            {
                genBeamCommandArgs.Add((("--from-manifest-id=\"" + this.fromManifestId) 
                                + "\""));
            }
            // If the runHealthChecks value was not default, then add it to the list of args.
            if ((this.runHealthChecks != default(bool)))
            {
                genBeamCommandArgs.Add(("--run-health-checks=" + this.runHealthChecks));
            }
            // If the redeploy value was not default, then add it to the list of args.
            if ((this.redeploy != default(bool)))
            {
                genBeamCommandArgs.Add(("--redeploy=" + this.redeploy));
            }
            // If the buildSequentially value was not default, then add it to the list of args.
            if ((this.buildSequentially != default(bool)))
            {
                genBeamCommandArgs.Add(("--build-sequentially=" + this.buildSequentially));
            }
            // If the merge value was not default, then add it to the list of args.
            if ((this.merge != default(bool)))
            {
                genBeamCommandArgs.Add(("--merge=" + this.merge));
            }
            // If the replace value was not default, then add it to the list of args.
            if ((this.replace != default(bool)))
            {
                genBeamCommandArgs.Add(("--replace=" + this.replace));
            }
            // If the toFile value was not default, then add it to the list of args.
            if ((this.toFile != default(string)))
            {
                genBeamCommandArgs.Add((("--to-file=\"" + this.toFile) 
                                + "\""));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual DeploymentPlanWrapper DeploymentPlan(DeploymentPlanArgs planArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("deployment");
            genBeamCommandArgs.Add("plan");
            genBeamCommandArgs.Add(planArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            DeploymentPlanWrapper genBeamCommandWrapper = new DeploymentPlanWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class DeploymentPlanWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual DeploymentPlanWrapper OnStreamDeploymentPlanMetadata(System.Action<ReportDataPoint<BeamDeploymentPlanMetadata>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
        public virtual DeploymentPlanWrapper OnBuildErrorsRunProjectBuildErrorStream(System.Action<ReportDataPoint<BeamRunProjectBuildErrorStream>> cb)
        {
            this.Command.On("buildErrors", cb);
            return this;
        }
        public virtual DeploymentPlanWrapper OnProgressPlanReleaseProgress(System.Action<ReportDataPoint<BeamPlanReleaseProgress>> cb)
        {
            this.Command.On("progress", cb);
            return this;
        }
    }
}
