Sharing a Microservice with Unity via UPM

## Dependencies

Before you can use Distribute a Microservice with UPM, you need to complete the 
[Getting-Started 
Guide](doc:cli-guide-getting-started). That means having [Dotnet 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed, and getting the  [Beam CLI](https://www.nuget.org/packages/Beamable.Tools).

You can confirm you have everything installed checking the versions of the tools.
```sh
dotnet --version
beam version # beam --version also works.
```

You also need to have a local `.beamable` workspace with a Beamable 
Standalone Microservice. As a reminder, you can create one quickly using the commands below.
```sh
beam init Project
cd Project
dotnet beam project new service Service
```

## Steps

Standalone Microservice projects can be distributed as Unity Package Manager 
(UPM) packages, which allows downstream Unity projects to re-use an existing 
Microservice. 

However, there are several configuration steps required in the Standalone 
Microservice project. 

### Preparing the Microservice

Follow these steps to convert an existing Standalone Microservice into a UPM 
compatible Microservice. 

1. Rename "services" folder to "services~"
2. Modify the `.sln` file to mirror the folder name change. 
3. Create a local `Assets` folder next to the `.beamable` folder
4. Create a `Runtime` folder and a `Runtime/Client` folder next to the `.beamable` folder
5. Create a file in the `.beamable` folder called `linked-projects.json`,
    ```json
    {
        "unityProjectsPaths": [
            "."
        ]
    }
    ```
   
   So far, your folder structure should at least have these files. 
   ```
   /Project
     /.beamable
       linked-projects.json
     /Assets
     /Runtime
       /Client
     /services~
       /Service
         Service.cs
         Service.csproj
         Program.cs
         Dockerfile
   ```

6. In the `Runtime/Client` folder, create a file called `Service.Client.
asmdef` (replace `"Service"` with your service name), and paste the following,
   ```json
   {
      "references":[
         "Beamable.Platform",
         "Unity.Beamable",
         "Unity.Beamable.Runtime.Common",
         "Unity.Beamable.Server.Runtime",
         "Unity.Beamable.Server.Runtime.Common",
         "Unity.Beamable.Customer.Common"
      ],
      "name": "Service.Client",
      "autoReferenced": true
   }
   ```
   Make sure to replace `"Service"` in the `"name"` field with your service
   name.
7. Now you are ready to generate the client code. To do this, run the 
   following command **AFTER** building your Microservice. This command 
   should be run from the `/services~/Service` folder. 
   ```sh
   dotnet beam project generate-client ./bin/Debug/net8.0/Service.dll --output-links --output-path-hints "Service=Runtime/Client/ServiceClient.cs" --logs v
   ```
8. You need to create a `package.json` file. Place it next to the `/Assets` 
   folder in the Microservice project. 
   ```json
      {
        "name": "com.service",
        "version": "0.0.0",
        "displayName": "Demo"
      }
   ```
   
Finally, your project structure should look similar to this, 

So far, your folder structure should at least have these files.
   ```
   /Project
     package.json
     /.beamable
       linked-projects.json
     /Assets
     /Runtime
       /Client
         ServiceClient.cs
         Service.Client.asmdef
     /services~
       /Service
         Service.cs
         Service.csproj
         Program.cs
         Dockerfile
   ```

### Preparing the Unity Project

Now that the Microservice is ready, in order to import it into a Unity 
project as a UPM package, follow these steps. 

1. Ensure that the Unity project is referencing Beamable's packages. 
   At least Beamable version `com.beamable` and `com.beamable.server` 2.1.3 is 
   required. 
2. In Unity Package Manager, add a package from disk, and select the path to 
   your created `package.json` from the previous steps. 
3. Modify the `.beamable/additional-project-paths.json` file in the Unity 
   project and add the path to the UPM project folder. 
   ```json
   ["/path/to/Project"]
   ```