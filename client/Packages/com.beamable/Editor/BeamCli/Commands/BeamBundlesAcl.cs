
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesAclArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle reference: <bundle-name>, <bundle-name>@<tag>, or <bundle-name>@sha256:<checksum> (a name or tag resolves to its checksum; a bare name means @latest)</summary>
        public string bundleRef;
        /// <summary>Visibility tier to widen to (each tier is a superset of the previous, not a list of realms): 'realm' = only this realm; 'org' = every realm in your customer; 'public' = every realm in every customer. A literal <cid>.<pid> / <cid> / * is also accepted</summary>
        public string scope;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the bundleRef value to the list of args.
            genBeamCommandArgs.Add(this.bundleRef.ToString());
            // If the scope value was not default, then add it to the list of args.
            if ((this.scope != default(string)))
            {
                genBeamCommandArgs.Add(("--scope=" + this.scope));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesAclWrapper BundlesAcl(BundlesAclArgs aclArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("acl");
            genBeamCommandArgs.Add(aclArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesAclWrapper genBeamCommandWrapper = new BundlesAclWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesAclWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesAclWrapper OnStreamBundleAclCommandOutput(System.Action<ReportDataPoint<BeamBundleAclCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
