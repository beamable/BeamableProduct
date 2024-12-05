Inject custom server logic into Beamable's framework.

## Dependencies

Before you can federate using Beamable Standalone Microservices, you need to complete the [Getting-Started Guide](doc:cli-guide-getting-started). That means having [Dotnet 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed, and getting the  [Beam CLI](https://www.nuget.org/packages/Beamable.Tools). 

You can confirm you have everything installed checking the versions of the tools.
```sh
dotnet --version
beam version # beam --version also works.
```

In order to federate using a Microservice, you also need to have a local `.beamable` workspace with a Beamable Standalone Microservice. As a reminder, you can create one quickly using the commands below.
```sh
beam init MyProject
cd MyProject
dotnet beam project new service HelloWorld
```


## Federation

Microservice _Federation_ is the ability to inject custom server logic in the middle of existing Beamable server functionality. Federation can be used to add custom behaviour to your game like supporting external identity auth providers, using a block chain as the backing data provider for player inventory, managing how match making works, and more. 

There are 3 types of federation. All of these federations have C# interfaces that define the types of functions that they require. 
1. `IFederatedLogin`
2. `IFederatedInventory`
3. `IFederatedGameServer`

A Microservice supports these federations when both of the following requirements are true.
1. The `Microservice` class includes the associated federation interface.
2. The Microservice's local `federations.json` file includes information about the federation. 

Starting with the first constraint, take the example below, a service that federates login functionality may have a class signature like the following.

```csharp
public partial class ExampleService : IFederatedLogin<MySample> 
{
	// implementation
}
```

The `MySample` class included as the generic type in the `IFederatedLogin` interface specifies the id for the federated login. The class signature for these types must implement the `IFederationId` type, and be annotated with a `FederationId` attribute. The string argument for the attribute constructor must be stable between releases of your service. It also needs to be unique.

```csharp
[FederationId("myId")]
public class MySample : IFederationId { }
```

The second constraint refers to the configuration file, `federations.json`. This file must specify that the `ExampleService` class federates `IFederatedLogin` with the unique name `"myId"`. If this is not true, then the Microservice should receive a compile error. 

> 📘 Make sure you have the source generator!
>
> The compile error that ensures the `federations.json` file is in-sync with the C# code comes from a custom source generator. If your files are out of date, and you are not getting compile errors, make sure your `.csproj` file has the `<PackageReference Include="Beamable.Microservice.SourceGen" Version="$(BeamableVersion)" OutputItemType="Analyzer" />` reference.

If the `federations.json` file is not accurate, you should see an error like this when attempting to compile or run the service. 

```sh
Error BEAM_FED_O001 : Missing declared Federation in MicroserviceFederationsConfig. Microservice=ExampleService, Id=myId, Interface=IFederatedLogin. Please add this Id by running `dotnet beam fed add MyExample myId IFederatedLogin` from your project's root directory. Or remove the IFederatedLogin that references myId  interface from the ExampleService Microservice class.
```

You should use the CLI to set the `federations.json` file, and it should look like the following, 

```json
{"federations":{"myId":[{"interface":"IFederatedLogin"}]}}
```

## CLI Commands

The CLI offers a few commands to enable and disable federations for a service. The `beam fed` command suite allows you to read and write federation data. None of the commands will modify your C# source files, so you must always make sure the class signature of the `Microservice` aligns with the `federations.json` file. 

The `beam fed list` command will show all federations for all services. 
```sh
dotnet beam fed list
 {                                                                               
    "cid": "1338004997867618",                                                   
    "pid": "DE_1754280032981028",                                                
    "services": [                                                                
       {                                                                         
          "beamoName": "ExampleService",                                         
          "routingKey": "chriss-macbook-pro-2_59e8e38ad189aefe093dfa7d74e18841", 
          "federations": {                                                       
             "myId": [                                                           
                {                                                                
                   "interface": "IFederatedLogin"                                
                }                                                                
             ]    
            
```

The `beam fed add` and `beam fed remove` commands will modify the `federations.json` file one service and federation at a time. If you need to set all the federations at once, use the `beam fed set` command. 