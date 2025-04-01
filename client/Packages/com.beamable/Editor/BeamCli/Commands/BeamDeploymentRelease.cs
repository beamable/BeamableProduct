
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class DeploymentReleaseArgs : Beamable.Common.BeamCli.IBeamCommandArgs
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
        /// <summary>The file path to a pre-generated plan file using the `deploy plan` command</summary>
        public string fromPlan;
        /// <summary>Relative path to the .sln file to use for the new project. If the .sln file does not exist, it will be created. When no option is configured, if this command is executing inside a .beamable folder, then the first .sln found in .beamable/.. will be used. If no .sln is found, the .sln path will be <name>.sln. If no .beamable folder exists, then the <project>/<project>.sln will be used</summary>
        public string sln;
        /// <summary>Use the most recent plan generated from the plan command</summary>
        public bool fromLatestPlan;
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
            // If the fromPlan value was not default, then add it to the list of args.
            if ((this.fromPlan != default(string)))
            {
                genBeamCommandArgs.Add((("--from-plan=\"" + this.fromPlan) 
                                + "\""));
            }
            // If the sln value was not default, then add it to the list of args.
            if ((this.sln != default(string)))
            {
                genBeamCommandArgs.Add((("--sln=\"" + this.sln) 
                                + "\""));
            }
            // If the fromLatestPlan value was not default, then add it to the list of args.
            if ((this.fromLatestPlan != default(bool)))
            {
                genBeamCommandArgs.Add(("--from-latest-plan=" + this.fromLatestPlan));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual DeploymentReleaseWrapper DeploymentRelease(DeploymentReleaseArgs releaseArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("deployment");
            genBeamCommandArgs.Add("release");
            genBeamCommandArgs.Add(releaseArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            DeploymentReleaseWrapper genBeamCommandWrapper = new DeploymentReleaseWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class DeploymentReleaseWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual DeploymentReleaseWrapper OnStreamDeploymentPlanMetadata(System.Action<ReportDataPoint<BeamDeploymentPlanMetadata>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
        public virtual DeploymentReleaseWrapper OnBuildErrorsRunProjectBuildErrorStream(System.Action<ReportDataPoint<BeamRunProjectBuildErrorStream>> cb)
        {
            this.Command.On("buildErrors", cb);
            return this;
        }
        public virtual DeploymentReleaseWrapper OnProgressPlanReleaseProgress(System.Action<ReportDataPoint<BeamPlanReleaseProgress>> cb)
        {
            this.Command.On("progress", cb);
            return this;
        }
    }
}
