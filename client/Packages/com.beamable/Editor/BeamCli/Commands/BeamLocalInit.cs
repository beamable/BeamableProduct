
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class LocalInitArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Path to write the manifest to (defaults to .beamable/local-stack.json)</summary>
        public string config;
        /// <summary>Overwrite an existing manifest without asking</summary>
        public bool force;
        /// <summary>Backend API host baked into the manifest</summary>
        public string host;
        /// <summary>Portal frontend URL baked into the manifest</summary>
        public string portalUrl;
        /// <summary>Absolute path to the BeamableAPI (C# gateway) repo</summary>
        public string apiDir;
        /// <summary>Absolute path to the BeamableBackend (Scala) repo</summary>
        public string scalaDir;
        /// <summary>Absolute path to the portal frontend repo</summary>
        public string portalDir;
        /// <summary>Comma/space separated Scala tools/* services to run</summary>
        public string scalaServices;
        /// <summary>Comma/space separated microservice ids to run</summary>
        public string services;
        /// <summary>Comma/space separated portal extension ids to run</summary>
        public string extensions;
        /// <summary>Only update the microservice/extension steps of an existing manifest, leaving everything else untouched</summary>
        public bool updateServices;
        /// <summary>JVM flags each Scala service is launched with; the heap cap keeps ~18 JDK 8 JVMs from each reserving a quarter of physical RAM</summary>
        public string scalaJvmArgs;
        /// <summary>Do not write the local web package registry steps (Verdaccio and local-unpkg); skips the portal-localdev prompt and leaves the manifest without them</summary>
        public bool noWebRegistry;
        /// <summary>Explicitly write the local web package registry steps; they are already the default, so this is a no-op except that it overrides --no-web-registry when both are passed</summary>
        public bool withWebRegistry;
        /// <summary>Absolute path to the portal-localdev directory holding the web registry compose file; skips the prompt for it</summary>
        public string webRegistryDir;
        /// <summary>Also write the beam-local-stack agent skill, documenting the repositories this manifest spans and how the stack works, to .claude/skills/ (off by default; regenerated on every init that passes this)</summary>
        public bool skill;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the config value was not default, then add it to the list of args.
            if ((this.config != default(string)))
            {
                genBeamCommandArgs.Add(("--config=" + this.config));
            }
            // If the force value was not default, then add it to the list of args.
            if ((this.force != default(bool)))
            {
                genBeamCommandArgs.Add(("--force=" + this.force));
            }
            // If the host value was not default, then add it to the list of args.
            if ((this.host != default(string)))
            {
                genBeamCommandArgs.Add(("--host=" + this.host));
            }
            // If the portalUrl value was not default, then add it to the list of args.
            if ((this.portalUrl != default(string)))
            {
                genBeamCommandArgs.Add(("--portal-url=" + this.portalUrl));
            }
            // If the apiDir value was not default, then add it to the list of args.
            if ((this.apiDir != default(string)))
            {
                genBeamCommandArgs.Add(("--api-dir=" + this.apiDir));
            }
            // If the scalaDir value was not default, then add it to the list of args.
            if ((this.scalaDir != default(string)))
            {
                genBeamCommandArgs.Add(("--scala-dir=" + this.scalaDir));
            }
            // If the portalDir value was not default, then add it to the list of args.
            if ((this.portalDir != default(string)))
            {
                genBeamCommandArgs.Add(("--portal-dir=" + this.portalDir));
            }
            // If the scalaServices value was not default, then add it to the list of args.
            if ((this.scalaServices != default(string)))
            {
                genBeamCommandArgs.Add(("--scala-services=" + this.scalaServices));
            }
            // If the services value was not default, then add it to the list of args.
            if ((this.services != default(string)))
            {
                genBeamCommandArgs.Add(("--services=" + this.services));
            }
            // If the extensions value was not default, then add it to the list of args.
            if ((this.extensions != default(string)))
            {
                genBeamCommandArgs.Add(("--extensions=" + this.extensions));
            }
            // If the updateServices value was not default, then add it to the list of args.
            if ((this.updateServices != default(bool)))
            {
                genBeamCommandArgs.Add(("--update-services=" + this.updateServices));
            }
            // If the scalaJvmArgs value was not default, then add it to the list of args.
            if ((this.scalaJvmArgs != default(string)))
            {
                genBeamCommandArgs.Add(("--scala-jvm-args=" + this.scalaJvmArgs));
            }
            // If the noWebRegistry value was not default, then add it to the list of args.
            if ((this.noWebRegistry != default(bool)))
            {
                genBeamCommandArgs.Add(("--no-web-registry=" + this.noWebRegistry));
            }
            // If the withWebRegistry value was not default, then add it to the list of args.
            if ((this.withWebRegistry != default(bool)))
            {
                genBeamCommandArgs.Add(("--with-web-registry=" + this.withWebRegistry));
            }
            // If the webRegistryDir value was not default, then add it to the list of args.
            if ((this.webRegistryDir != default(string)))
            {
                genBeamCommandArgs.Add(("--web-registry-dir=" + this.webRegistryDir));
            }
            // If the skill value was not default, then add it to the list of args.
            if ((this.skill != default(bool)))
            {
                genBeamCommandArgs.Add(("--skill=" + this.skill));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual LocalInitWrapper LocalInit(LocalInitArgs initArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("local");
            genBeamCommandArgs.Add("init");
            genBeamCommandArgs.Add(initArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            LocalInitWrapper genBeamCommandWrapper = new LocalInitWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class LocalInitWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual LocalInitWrapper OnStreamLocalStackInitCommandResult(System.Action<ReportDataPoint<BeamLocalStackInitCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
