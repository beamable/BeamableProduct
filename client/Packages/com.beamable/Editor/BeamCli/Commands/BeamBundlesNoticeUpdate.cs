
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesNoticeUpdateArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle name, optionally namespaced as @<namespace>/<bundle-name></summary>
        public string bundleName;
        /// <summary>The notice to edit, as `bundles releases` lists it</summary>
        public string noticeId;
        /// <summary>Deprecations: a later end of support (UTC date)</summary>
        public string endOfSupport;
        /// <summary>Deprecations: a later end of life (UTC date)</summary>
        public string endOfLife;
        /// <summary>Security advisories: a new severity, low, medium, high or critical</summary>
        public string severity;
        /// <summary>Security advisories: a new advisory id. An empty value clears it</summary>
        public string advisoryId;
        /// <summary>Security advisories: a later update-by date, or a first one (UTC date)</summary>
        public string updateBy;
        /// <summary>Retirements: a later end of acquisitions (UTC date)</summary>
        public string acquisitionsEnd;
        /// <summary>A new message</summary>
        public string message;
        /// <summary>A new link. An empty value clears it</summary>
        public string link;
        /// <summary>A new replacement version</summary>
        public System.Nullable<long> replacementVersion;
        /// <summary>A new replacement tag, instead of a version. An empty value clears the replacement</summary>
        public string replacementTag;
        /// <summary>Resolve a security advisory, known issue or announcement: it stops warning and acting, and can't be edited again. Deprecations and retirements are withdrawn instead</summary>
        public bool resolve;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the bundleName value to the list of args.
            genBeamCommandArgs.Add(this.bundleName.ToString());
            // Add the noticeId value to the list of args.
            genBeamCommandArgs.Add(this.noticeId.ToString());
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
            // If the resolve value was not default, then add it to the list of args.
            if ((this.resolve != default(bool)))
            {
                genBeamCommandArgs.Add(("--resolve=" + this.resolve));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesNoticeUpdateWrapper BundlesNoticeUpdate(BundlesNoticeUpdateArgs updateArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("notice");
            genBeamCommandArgs.Add("update");
            genBeamCommandArgs.Add(updateArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesNoticeUpdateWrapper genBeamCommandWrapper = new BundlesNoticeUpdateWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesNoticeUpdateWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesNoticeUpdateWrapper OnStreamBundleNoticeCommandOutput(System.Action<ReportDataPoint<BeamBundleNoticeCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
