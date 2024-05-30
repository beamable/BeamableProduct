Welcome to the monorepo, friend. Stay awhile, and pay attention :smile: 

---

This repository has the following products, 
1. the CLI
2. the Unity SDK
3. the Templates
4. the Microservice Framework

This repository also has a lot of supporting libraries. Some important ones to call out include, 
1. the Terraform
2. the Perf Test Standalone Microservice
3. the Build Scripts

There are some strange inter-dependencies between the various products and libraries that you should be aware of. Especially if you plan on developing the Unity SDK, you will need to know about the "Unity Common Konudrum (UCK)"


# The CLI Solution

The Beam CLI is a dotnet solution located in the `/cli` folder. Confusingly, there is also a dotnet _project_ in `/cli/cli` which is the actual executable CLI project. This CLI project is the dotnet tool we publish to Nuget, [https://www.nuget.org/packages/Beamable.Tools](https://www.nuget.org/packages/Beamable.Tools)

Within the `/cli` solution, there are also a few other project folders, 

| path                          | note                                                                                                                     |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| `/cli/beamable.common`        | this is shared code between Unity, the CLI, and the Microservice Framework. Any edits to this project are wide-reaching. |
| `/cli/beamable.server.common` | this is shared code between Unity, the CLI, and the Microservice Framework. Any edits to this project are wide-reaching. |
| `/cli/tests`                  | this is a unit test project for the CLI executable.                                                                      |

Within the CLI project itself, the application is built on top of Microsoft's Command Line Library, [https://learn.microsoft.com/en-us/dotnet/standard/commandline/](https://learn.microsoft.com/en-us/dotnet/standard/commandline/). Beyond that, we've introduced our own dependency injection style framework that groups "Commands" in a service-scope, and "Services" within a separate scope. To start understanding the flow of the program, I'd recommend looking at the `Program` class, which functions as the entry point to the executable. 

To develop within the CLI, it is important to know how to execute the CLI. There are 4 primary ways to execute commands to test them out. 

1. [Run via `dotnet`](#running-cli-via-dotnet)
2. [Run via the IDE](#running-cli-via-ide)
3. [Run by installing the entire CLI globally on your machine](running-cli-via-global-install)
4. [Run a unit test in the `/cli/tests` project.](#running-cli-via-tests)

The CLI makes use of the `/cli/beamable.common` and `/cli/beamable.server.common` folders, which are source-shared with the Unity SDK. When those projects are built, there are custom msbuild targets that will copy the code into the `/client/Packages/` sub folders into the Unity SDK. If you see errors during the build from the target, `CopyCodeToUnity`, something with this flow has gone wrong. 



#### Running CLI via Dotnet

the CLI is a dotnet project, so you can run it like any other dotnet project in development. This approach can be useful if your a command-line junkie.  

Either open a terminal to the `/cli/cli` folder, or pass the `--project cli/cli`  if the terminal is open to the monorepo root. Then you can run `dotnet run`, and pass program arguments after the `--` symbol. For example, the following would run the `config` command.

```sh
# inside the cli project folder,
dotnet run -- config
```

#### Running CLI via IDE

You can put breakpoints in the CLI and use the IDE to debug/step-through your program. In Rider, open the Run Configurations window, and set the `Program Arguments` to the command string you want to test. For example, if you wanted to debug the `config` command, you could set the `Program Arguments` to `config`. Then, it may be helpful to set the `Working Directory` to an existing `.beamable` workspace folder. 

#### Running CLI via global install

Perhaps the slowest approach to test your changes in the CLI, but also perhaps the most bullet-proof for longer QA testing rounds, is to install your local source version of the CLI as the globally installed dotnet tool. This way, you can run `beam` commands from the CLI directly, and you'll be using your version of the CLI. 

There is an `install.sh` script in the `/cli/cli` folder. If you run it, it will
1. build your local CLI to a package
2. uninstall any existing global dotnet tool for `beam`, 
3. install the package from step 1 as the global tool. 

You should see these logs at the end if all was successful.
```
Tool 'beamable.tools' (version '1.19.17') was successfully uninstalled.
You can invoke the tool using the following command: beam
Tool 'beamable.tools' (version '0.0.0') was successfully installed.
```

Now you can run `beam` commands. 

```
chrishanna@Chriss-MacBook-Pro-2 cli % beam --version
1.0.0+10f79bc47ac140588701a426c1a1b4eaee57b654
```

It is okay that the version number reports as 1.0.0 in this flow. 

#### Running CLI via Tests

First of all, bless you for writing a test. One major benefit of this approach is that you can cement your workflow and testing strategy as code and then re-use that effort for all future CI/CD testing pipelines in the future <3 . 

Though, it can be frustrating to write a test. There is an existing test project in `cli/tests`. 

Sometimes you get lucky as a developer and you can implement your tasking in a `public static` function with zero dependencies. If this is true for you, I recommend making a test that ONLY calls the bare-minimum amount of set up logic. If you can get away with writing a test like this, then I highly encourage it.

```csharp
[TestCase(true, new[] { "a", "b" }, new[] { "b", "a" })]  
[TestCase(false, new[] { "a" }, new[] { "b" })]  
public void Equality_Required(bool expected, string[] a, string[] b)  
{  
    var schemaA = new OpenApiSchema { Required = new HashSet<string>(a) };  
    var schemaB = new OpenApiSchema { Required = new HashSet<string>(b) };  
  
    var isEqual = NamedOpenApiSchema.AreEqual(schemaA, schemaB, out var differences);  
    foreach (var diff in differences)  
    {       Console.WriteLine(diff);  
    }    Assert.AreEqual(expected, isEqual);  
}
```

However, in a lot of cases, the actual test is running a command via the CLI, and making assertions about the output of the command, as well as the file state of the `.beamable` folder. In this case, I'd encourage you to look at the `BeamInitFlows` class an example of this more "integration testing" approach. 

If you have a test that is a sub-class of `CLITest`, then your tests will automatically, 
1. set up some network mocking, 
2. set up some log mocking, 
3. create a temporary folder for you

This means that you can exercise a command by running, 
```csharp
Run("config");
```

And then the _actual_ `config` command will run as if you ran it via the CLI in your test's temp folder. 

A common "gotcha" here is that networking is not allowed by default. Communication with _Docker_ is allowed, so Docker must be running for the tests to succeed. However, because networking is not allowed, you'll need to set up mock calls to any API resources the code execution requires. For example, this snippet sets up a proxy to allow a `Login` call to avoid networking, and return this hardcoded value,

```csharp
Mock<IAuthApi>(mock =>  
{  
    mock.Setup(x => x.Login(userName, password, false, false))  
       .ReturnsPromise(new TokenResponse  
       {  
          refresh_token = "refresh",  
          access_token = "access",  
          token_type = "token"  
       })  
       .Verifiable();  
});
```




# Unity SDK

There is a Unity project inside the `/client` folder. The Unity SDK itself is a Unity Package Manager (UPM) Package. Per UPM regulation, the source code is inside `/client/Packages/com.beamable`. As of this writing, there is a second UPM package, `/client/Packages/com.beamable.server`. However, the plan is to move the contents of the `.server` package into the `com.beamable` one. 

There are a few unique pieces of our SDK that are worth calling immediate attention. 

First, some of the code is copied directly from the `/cli/beamable.common` and `/cli/beamable.server.common` projects. More on this later, but the sub folders in the `/Packages` folder that are copied are as follows, 

| Package Path                                         | Copied From                                |
| ---------------------------------------------------- | ------------------------------------------ |
| `client/Packages/com.beamable/Common/Runtime`        | `cli/beamable.common/Runtime`              |
| `client/Packages/com.beamable.server/Runtime/Common` | `cli/beamable.server.common/Runtime`       |
| `client/Packages/com.beamable.server/SharedRuntime/` | `cli/beamable.server.common/SharedRuntime` |

 Second, thats not the only code that you shouldn't modify directly! There is auto-generated code inside the Unity SDK. If you edit the auto-generated code, then your changes will get lost eventually when the code is regenerated. Some of the code inside the `com.beamable/Common/Runtime` folder is auto-generated, but you already shouldn't be modifying that since its copied from `cli/beamable.common/Runtime`. There is also auto-generated code that helps the Unity SDK communicate with the Beam CLI tool. The bindings for the various commands are auto-generated and stored in the `/client/Packages/com.beamable/Editor/BeamCli/Commands` folder. 
 
 Third, the `client/Packages/com.beamable/Runtime/Environment/Resources` folder contains `.json` files with connection strings to Beamable, and other important configuration. 

```json
// env-default.json controls the connection strings for Beamable.
{
  "environment": "dev",
  "apiUrl": "https://dev.api.beamable.com",
  "portalUrl": "https://dev-portal.beamable.com",
  "beamMongoExpressUrl": "https://dev.storage.beamable.com",
  "dockerRegistryUrl": "https://dev-microservices.beamable.com/v2/",
  "isUnityVsp": "UNITY__VSP__UID",
  "sdkVersion": "BUILD__SDK__VERSION__STRING"
}
```

And there is a second file, 
```json
// versions-default.json controls which version of the common nuget packages to use
{
	"nugetPackageVersion": "0.0.0-PREVIEW.NIGHTLY-202405141737"
}
```

If you are developing within the Unity SDK, and you need to target production-level Beamable (api.beamable.com), then you must change the contents of the `env-default.json` file. You can also use the "Window/Beamable/Utilities/Change Environment" option within the Editor to create an override file. See the `BeamableEnvironment` class for details on these values are resolved. 

The `nugetPackageVersion` property in the `versions-default.json` specifies the nuget dependencies of the SDK. The SDK requires,
1. the source code from `/cli/beamable.common`,
2. the source code from `/cli/beamable.server.common`, 
3. a version of the beam CLI. 

Luckily, all of those things share the same Nuget version numbering scheme. If you want to use a specific version, or you are upgrading the Unity SDK to use a newer version of the Nuget packages, you need to
1. change the version number in the `versions-default.json` file, to a valid deployed Nuget version, and then
2. run the `beam unity download-all-nuget-packages ./client --logs v`. This will download the source code and copy the files into the Unity project. 

However, if you want to use your local files to validate something, set the `nugetPackageVersion` to `0.0.123`, which is our "local dev" version. Then, when you build the CLI, include the `ReleaseSharedCode` msbuild property, and it will copy the local source code into the Unity project. 

```sh
dotnet build -p:ReleaseSharedCode=true
```

Finally, as mentioned, the SDK depends on the Beam CLI. As part of the SDK's initialization, it will resolve a Beam CLI executable path. In a deployed package, it uses the value of the `nugetPackageVersion` field and downloads the Nuget tool as a local package in `/Library/BeamableEditor/BeamCLI`. If you are developing locally, you can edit this through the `EditorConfiguration` .

# Releases

### Releasing 1.x patches

In the 1.x.y days of Beamable, we used Jenkins to make deployments of the Unity SDK, CLI, Base Docker Image (which we no longer even have), and Nuget packages. While hopefully rare, we still may need to deploy 1.x.y releases every so often. 

The first step is to get the code you want to deploy onto the `production-1-19-0` branch. It will likely be dicey, but I recommend 
1. checking out `production-1-19-0`, forking a branch called `patch/1-x-y` (where `x` and `y` are your release numbers), 
2. cherry picking commits from the main repo back into the branch, 
3. opening a PR from your branch back into `production-1-19-0` and letting the tests run and the team stare at the changes for awhile. 

Then, when it is time to release, navigate to Jenkins, [https://db-jenkins.disruptorbeam.com/](https://db-jenkins.disruptorbeam.com/). 
If you're making a Release Candidate, select the "Beamable_Staging" job, or if you're making a full Production Release, select the "Beamable_Production" job.

Inside the job, on the left-hand menu, select, "Build with Parameters",
1. ensure the `SOURCE_BRANCH` is set to "production-1-19-0"
2. ensure the `VERSION` is set correctly for your version. If you're releasing 1.19.500, then manually enter that in the `VERSION` field. Do not enter the release candidate number if you're making a staging build (that will be added automatically). 