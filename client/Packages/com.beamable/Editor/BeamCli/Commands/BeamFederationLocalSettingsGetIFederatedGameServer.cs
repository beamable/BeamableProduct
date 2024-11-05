
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class FederationLocalSettingsGetIFederatedGameServerArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The Beamo ID for the microservice whose federation you want to configure</summary>
        public string beamoId;
        /// <summary>The Federation ID for the federation instance you want to configure</summary>
        public string fedId;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the beamoId value was not default, then add it to the list of args.
            if ((this.beamoId != default(string)))
            {
                genBeamCommandArgs.Add((("--beamo-id=\"" + this.beamoId) 
                                + "\""));
            }
            // If the fedId value was not default, then add it to the list of args.
            if ((this.fedId != default(string)))
            {
                genBeamCommandArgs.Add((("--fed-id=\"" + this.fedId) 
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
        public virtual FederationLocalSettingsGetIFederatedGameServerWrapper FederationLocalSettingsGetIFederatedGameServer(FederationLocalSettingsGetIFederatedGameServerArgs iFederatedGameServerArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("federation");
            genBeamCommandArgs.Add("local-settings");
            genBeamCommandArgs.Add("get");
            genBeamCommandArgs.Add("IFederatedGameServer");
            genBeamCommandArgs.Add(iFederatedGameServerArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            FederationLocalSettingsGetIFederatedGameServerWrapper genBeamCommandWrapper = new FederationLocalSettingsGetIFederatedGameServerWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class FederationLocalSettingsGetIFederatedGameServerWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual FederationLocalSettingsGetIFederatedGameServerWrapper OnStreamLocalSettings_IFederatedGameServer(System.Action<ReportDataPoint<Beamable.Common.LocalSettings_IFederatedGameServer>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
