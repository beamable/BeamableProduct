
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesNoticeCreateArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle name, optionally namespaced as @<namespace>/<bundle-name></summary>
        public string bundleName;
        /// <summary>The notice kind: deprecation (needs versions, --end-of-support and --end-of-life), security-advisory (versions and --severity), known-issue (versions), retirement (the whole bundle, --acquisitions-end) or announcement (versions or the whole bundle)</summary>
        public string kind;
        /// <summary>The first version the notice covers. Leave out both versions for the whole bundle</summary>
        public long minVersion;
        /// <summary>The last version the notice covers</summary>
        public long maxVersion;
        /// <summary>Deprecations: when new installs of these versions stop (UTC date)</summary>
        public string endOfSupport;
        /// <summary>Deprecations: when installations still on these versions are updated (UTC date)</summary>
        public string endOfLife;
        /// <summary>Security advisories: low, medium, high or critical. A critical one refuses new installs of the versions</summary>
        public string severity;
        /// <summary>Security advisories: an id such as a CVE</summary>
        public string advisoryId;
        /// <summary>Security advisories: when installations still on these versions are moved to the fix (UTC date). Needs a replacement</summary>
        public string updateBy;
        /// <summary>Retirements: when new acquisitions of the bundle stop (UTC date)</summary>
        public string acquisitionsEnd;
        /// <summary>What the orgs running these versions need to know</summary>
        public string message;
        /// <summary>A page with more detail</summary>
        public string link;
        /// <summary>The version orgs should move to, or the one with the fix</summary>
        public System.Nullable<long> replacementVersion;
        /// <summary>The tag orgs should move to, instead of a version</summary>
        public string replacementTag;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the bundleName value to the list of args.
            genBeamCommandArgs.Add(this.bundleName.ToString());
            // If the kind value was not default, then add it to the list of args.
            if ((this.kind != default(string)))
            {
                genBeamCommandArgs.Add(("--kind=" + this.kind));
            }
            // If the minVersion value was not default, then add it to the list of args.
            if ((this.minVersion != default(long)))
            {
                genBeamCommandArgs.Add(("--min-version=" + this.minVersion));
            }
            // If the maxVersion value was not default, then add it to the list of args.
            if ((this.maxVersion != default(long)))
            {
                genBeamCommandArgs.Add(("--max-version=" + this.maxVersion));
            }
            // If the endOfSupport value was not default, then add it to the list of args.
            if ((this.endOfSupport != default(string)))
            {
                genBeamCommandArgs.Add(("--end-of-support=" + this.endOfSupport));
            }
            // If the endOfLife value was not default, then add it to the list of args.
            if ((this.endOfLife != default(string)))
            {
                genBeamCommandArgs.Add(("--end-of-life=" + this.endOfLife));
            }
            // If the severity value was not default, then add it to the list of args.
            if ((this.severity != default(string)))
            {
                genBeamCommandArgs.Add(("--severity=" + this.severity));
            }
            // If the advisoryId value was not default, then add it to the list of args.
            if ((this.advisoryId != default(string)))
            {
                genBeamCommandArgs.Add(("--advisory-id=" + this.advisoryId));
            }
            // If the updateBy value was not default, then add it to the list of args.
            if ((this.updateBy != default(string)))
            {
                genBeamCommandArgs.Add(("--update-by=" + this.updateBy));
            }
            // If the acquisitionsEnd value was not default, then add it to the list of args.
            if ((this.acquisitionsEnd != default(string)))
            {
                genBeamCommandArgs.Add(("--acquisitions-end=" + this.acquisitionsEnd));
            }
            // If the message value was not default, then add it to the list of args.
            if ((this.message != default(string)))
            {
                genBeamCommandArgs.Add(("--message=" + this.message));
            }
            // If the link value was not default, then add it to the list of args.
            if ((this.link != default(string)))
            {
                genBeamCommandArgs.Add(("--link=" + this.link));
            }
            // If the replacementVersion value was not default, then add it to the list of args.
            if ((this.replacementVersion != default(System.Nullable<long>)))
            {
                genBeamCommandArgs.Add(("--replacement-version=" + this.replacementVersion));
            }
            // If the replacementTag value was not default, then add it to the list of args.
            if ((this.replacementTag != default(string)))
            {
                genBeamCommandArgs.Add(("--replacement-tag=" + this.replacementTag));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesNoticeCreateWrapper BundlesNoticeCreate(BundlesNoticeCreateArgs createArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("notice");
            genBeamCommandArgs.Add("create");
            genBeamCommandArgs.Add(createArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesNoticeCreateWrapper genBeamCommandWrapper = new BundlesNoticeCreateWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesNoticeCreateWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesNoticeCreateWrapper OnStreamBundleNoticeCommandOutput(System.Action<ReportDataPoint<BeamBundleNoticeCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
