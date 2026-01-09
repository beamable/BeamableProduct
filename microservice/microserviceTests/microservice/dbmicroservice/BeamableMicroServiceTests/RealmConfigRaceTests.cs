using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Beamable.Server.Api.RealmConfig;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests;

public class RealmConfigRaceTests : CommonTest
{
    [Test]
    [NonParallelizable]
    public async Task AddARealmConfigAfterRunning()
    {

        TestSocket testSocket = null;
        var ms = new TestSetup(new TestSocketProvider(socket =>
        {
            testSocket = socket;
            socket
                .AddAuthMessageHandlers()
                .AddProviderMessageHandlers(1, true)
	                
                .AddMessageHandler(
                    MessageMatcher
                        .WithReqId(1)
                        .WithStatus(200)
                        .WithPayload<int>(n => n == 1), // the size of the realm config should be 1 !
                    MessageResponder.NoResponse(),
                    MessageFrequency.OnlyOnce()
                )
                
                .AddMessageHandler(
                    res =>
                    {
                        if (res.id == -3 && res.path.Contains("realms/config"))
                        {
                            return true;
                        }
                        return false;
                    }, 
                    MessageResponder.Success(new GetRealmConfigResponse
                    {
                        config = new Dictionary<string, string>()
                    }),
                    MessageFrequency.OnlyOnce()
                    )
                
                .AddMessageHandler(
                    res =>
                    {
                        // this is the second request that goes out to get the realm config.
                        if (res.id == -7 && res.path.Contains("realms/config"))
                        {
                            return true;
                        }
                        return false;
                    }, 
                    MessageResponder.Success(new GetRealmConfigResponse
                    {
                        config = new Dictionary<string, string>
                        {
                            ["namespace|key"] = "value"
                        }
                    }),
                    MessageFrequency.OnlyOnce()
                )
                ;
        }));
        await ms.Start<RealmRaceService>(new TestArgs(), builder =>
        {
            builder.RemoveIfExists<IRealmConfigService>();
            builder.RemoveIfExists<IMicroserviceRealmConfigService>();
            builder.AddSingleton<IRealmConfigService, RealmConfigService>();
            builder.AddSingleton<IMicroserviceRealmConfigService>(x => x.GetService<IRealmConfigService>());
            
        });
        Assert.IsTrue(ms.HasInitialized);

        testSocket.SendToClient(ClientRequest.ClientCallable("micro_race", nameof(RealmRaceService.Example), 1, 1));

        // simulate shutdown event...
        await ms.OnShutdown(this, null);
        Assert.IsTrue(testSocket.AllMocksCalled());
    }
    
    [Test]
    [NonParallelizable]
    public async Task AbuseTheSystem()
    {

        TestSocket testSocket = null;
        var ms = new TestSetup(new TestSocketProvider(socket =>
        {
            testSocket = socket;
            socket
                .AddAuthMessageHandlers()
                .AddProviderMessageHandlers(1, true)
	                
                // // the first /Refresh endpoint should be fine.
                // .AddMessageHandler(
                //     MessageMatcher
                //         .WithReqId(1)
                //         .WithStatus(200),
                //     MessageResponder.NoResponse(),
                //     MessageFrequency.OnlyOnce()
                // )
                
                // the call to the /Abuse endpoint
                .AddMessageHandler(
                    MessageMatcher
                        .WithReqId(1)
                        .WithStatus(200)
                        .WithPayload<int>(n => n == 0), // the method should return with a zero exit code
                    MessageResponder.NoResponse(),
                    MessageFrequency.OnlyOnce()
                )
                
                // the call to the refresh endpoint should complete fine.
                .AddMessageHandler(
                    MessageMatcher
                        .WithReqId(2)
                        .WithStatus(200),
                    MessageResponder.NoResponse(),
                    MessageFrequency.OnlyOnce()
                )
                
                // this is the initial realm config that the MS uses to boot with.
                .AddMessageHandler(
                    res =>
                    {
                        if (res.id == -3 && res.path.Contains("realms/config"))
                        {
                            return true;
                        }
                        return false;
                    }, 
                    MessageResponder.Success(new GetRealmConfigResponse
                    {
                        config = new Dictionary<string, string>
                        {
                            ["namespace|1"] = "a",
                            ["namespace|2"] = "a",
                            ["namespace|3"] = "a",
                            ["namespace|4"] = "a",
                            ["namespace|5"] = "a",
                            ["namespace|6"] = "a",
                            ["namespace|7"] = "a",
                            ["namespace|8"] = "a",
                            ["namespace|9"] = "a",
                            ["namespace|10"] = "a",
                        }
                    }),
                    MessageFrequency.OnlyOnce()
                    )
                
                // this is the request made from within the /Abuse method
                .AddMessageHandler(
                    res =>
                    {
                        // this is the second request that goes out to get the realm config.
                        if (res.id == -7 && res.path.Contains("realms/config"))
                        {
                            return true;
                        }
                        return false;
                    }, 
                    MessageResponder.Success(new GetRealmConfigResponse
                    {
                        config = new Dictionary<string, string>
                        {
                            ["namespace|1"] = "a",
                            ["namespace|2"] = "a",
                            ["namespace|3"] = "a",
                            ["namespace|4"] = "a",
                            ["namespace|5"] = "a",
                            ["namespace|6"] = "a",
                            ["namespace|7"] = "a",
                            ["namespace|8"] = "a",
                            ["namespace|9"] = "a",
                            ["namespace|10"] = "a",
                        }
                    }),
                    MessageFrequency.OnlyOnce()
                )
                
                // this is the request made from within the /Refresh method
                .AddMessageHandler(
                    res =>
                    {
                        // this is the second request that goes out to get the realm config.
                        if (res.id == -8 && res.path.Contains("realms/config"))
                        {
                            return true;
                        }
                        return false;
                    }, 
                    MessageResponder.Success(new GetRealmConfigResponse
                    {
                        config = new Dictionary<string, string>
                        {
                            ["namespace|1"] = "a",
                            ["namespace|2"] = "a",
                            ["namespace|3"] = "a",
                            ["namespace|4"] = "a",
                            ["namespace|5"] = "a",
                            ["namespace|6"] = null, // uh oh!
                            ["namespace|7"] = "a",
                            ["namespace|8"] = "a",
                            ["namespace|9"] = "a",
                            ["namespace|10"] = "a",
                        }
                    }),
                    MessageFrequency.OnlyOnce()
                )
                ;
        }));
        await ms.Start<RealmRaceService>(new TestArgs(), builder =>
        {
            builder.RemoveIfExists<IRealmConfigService>();
            builder.RemoveIfExists<IMicroserviceRealmConfigService>();
            builder.AddSingleton<IRealmConfigService, RealmConfigService>();
            builder.AddSingleton<IMicroserviceRealmConfigService>(x => x.GetService<IRealmConfigService>());
            
        });
        Assert.IsTrue(ms.HasInitialized);

        
        // // start by setting all of the realm config settings
        // testSocket.SendToClient(ClientRequest.ClientCallable("micro_race", nameof(RealmRaceService.RefreshRealmConfig), 1, 1));
        
        // start abusing the socket...
        testSocket.SendToClient(ClientRequest.ClientCallable("micro_race", nameof(RealmRaceService.Abuse), 1, 1));

        await Task.Delay(250); // wait a moment for the method to be mid-swing..
        
        // then trigger the realm config to get updated. 
        testSocket.SendToClient(ClientRequest.ClientCallable("micro_race", nameof(RealmRaceService.RefreshRealmConfig), 2, 1));

        // simulate shutdown event...
        await ms.OnShutdown(this, null);
        Assert.IsTrue(testSocket.AllMocksCalled());
    }
    
    [System.Serializable]
    private class GetRealmConfigResponse
    {
        // ReSharper disable once InconsistentNaming
        public Dictionary<string, string> config;
    }
    
   [Microservice("race", UseLegacySerialization = true, EnableEagerContentLoading = false)]
    public class RealmRaceService : Microservice
    {
        [ClientCallable]
        public async Promise<int> Example()
        {
            var config = Services.RealmConfig;
            var settings = await config.GetRealmConfigSettings();
            return settings.Count;
        }

        [ClientCallable]
        public async Promise RefreshRealmConfig()
        {
            _ = await Services.RealmConfig.GetRealmConfigSettings();
        }

        [ClientCallable]
        public async Promise<int> Abuse()
        {
            var config = await Services.RealmConfig.GetRealmConfigSettings();
            async Task<int> CountNonNull()
            {
                int nonNullCount = 0;
                for (var i = 1; i <= 10; i++)
                {
                    var value = config.GetSetting("namespace", i.ToString(), null);
                    if (!string.IsNullOrEmpty(value))
                    {
                        nonNullCount++;
                    }

                    await Task.Delay(1);
                }

                return nonNullCount;
            }

            var startCount = await CountNonNull();
            for (var i = 0; i < 200; i++)
            {
                var currentCount = await CountNonNull();
                if (currentCount != startCount)
                {
                    return 1; // return failure.
                }

                await Task.Delay(10); // wait a moment... 
            }

            return 0; // return success
        }
    }
}