//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Beamable.Server.Clients
{
    using System;
    using Beamable.Platform.SDK;
    using Beamable.Server;
    
    
    /// <summary> A generated client for <see cref="Beamable.LootBoxService.LootBoxService"/> </summary
    public sealed class LootBoxServiceClient : MicroserviceClient, Beamable.Common.IHaveServiceName
    {
        
        public LootBoxServiceClient(BeamContext context = null) : 
                base(context)
        {
        }
        
        public string ServiceName
        {
            get
            {
                return "LootBoxService";
            }
        }
        
        /// <summary>
        /// Call the GetTimeLeft method on the LootBoxService microservice
        /// <see cref="Beamable.LootBoxService.LootBoxService.GetTimeLeft"/>
        /// </summary>
        public Beamable.Common.Promise<double> GetTimeLeft()
        {
            System.Collections.Generic.Dictionary<string, object> serializedFields = new System.Collections.Generic.Dictionary<string, object>();
            return this.Request<double>("LootBoxService", "GetTimeLeft", serializedFields);
        }
        
        /// <summary>
        /// Call the Claim method on the LootBoxService microservice
        /// <see cref="Beamable.LootBoxService.LootBoxService.Claim"/>
        /// </summary>
        public Beamable.Common.Promise<bool> Claim()
        {
            System.Collections.Generic.Dictionary<string, object> serializedFields = new System.Collections.Generic.Dictionary<string, object>();
            return this.Request<bool>("LootBoxService", "Claim", serializedFields);
        }
    }
    
    internal sealed class MicroserviceParametersLootBoxServiceClient
    {
    }
    
    [BeamContextSystemAttribute()]
    public static class ExtensionsForLootBoxServiceClient
    {
        
        [Beamable.Common.Dependencies.RegisterBeamableDependenciesAttribute()]
        public static void RegisterService(Beamable.Common.Dependencies.IDependencyBuilder builder)
        {
            builder.AddScoped<LootBoxServiceClient>();
        }
        
        public static LootBoxServiceClient LootBoxService(this Beamable.Server.MicroserviceClients clients)
        {
            return clients.GetClient<LootBoxServiceClient>();
        }
    }
}