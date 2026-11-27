
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesCreateArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle name, optionally namespaced as @<namespace>/<bundle-name></summary>
        public string bundleName;
        /// <summary>Private (only your org) or public (listed in the marketplace once it has a release)</summary>
        public string visibility;
        /// <summary>The name the marketplace shows</summary>
        public string displayName;
        /// <summary>One line about what the bundle does</summary>
        public string summary;
        /// <summary>The marketplace description</summary>
        public string description;
        /// <summary>A marketplace category (repeatable)</summary>
        public string[] category;
        /// <summary>An icon for the listing</summary>
        public string iconUrl;
        /// <summary>A link to the bundle's documentation</summary>
        public string docsUrl;
        /// <summary>Require auto-update on every new installation of this bundle</summary>
        public System.Nullable<bool> autoUpdateRequired;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the bundleName value to the list of args.
            genBeamCommandArgs.Add(this.bundleName.ToString());
            // If the visibility value was not default, then add it to the list of args.
            if ((this.visibility != default(string)))
            {
                genBeamCommandArgs.Add(("--visibility=" + this.visibility));
            }
            // If the displayName value was not default, then add it to the list of args.
            if ((this.displayName != default(string)))
            {
                genBeamCommandArgs.Add(("--display-name=" + this.displayName));
            }
            // If the summary value was not default, then add it to the list of args.
            if ((this.summary != default(string)))
            {
                genBeamCommandArgs.Add(("--summary=" + this.summary));
            }
            // If the description value was not default, then add it to the list of args.
            if ((this.description != default(string)))
            {
                genBeamCommandArgs.Add(("--description=" + this.description));
            }
            // If the category value was not default, then add it to the list of args.
            if ((this.category != default(string[])))
            {
                for (int i = 0; (i < this.category.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--category=" + this.category[i]));
                }
            }
            // If the iconUrl value was not default, then add it to the list of args.
            if ((this.iconUrl != default(string)))
            {
                genBeamCommandArgs.Add(("--icon-url=" + this.iconUrl));
            }
            // If the docsUrl value was not default, then add it to the list of args.
            if ((this.docsUrl != default(string)))
            {
                genBeamCommandArgs.Add(("--docs-url=" + this.docsUrl));
            }
            // If the autoUpdateRequired value was not default, then add it to the list of args.
            if ((this.autoUpdateRequired != default(System.Nullable<bool>)))
            {
                genBeamCommandArgs.Add(("--auto-update-required=" + this.autoUpdateRequired));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesCreateWrapper BundlesCreate(BundlesCreateArgs createArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("create");
            genBeamCommandArgs.Add(createArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesCreateWrapper genBeamCommandWrapper = new BundlesCreateWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesCreateWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesCreateWrapper OnStreamBundleListingCommandOutput(System.Action<ReportDataPoint<BeamBundleListingCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
