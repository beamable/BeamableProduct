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

# Know Your Scripts

The codebase has a bunch of build utility scripts that you need to understand to compose your workflow. In some cases, you'll be able to get away without running the scripts, if your work can be done inside unity tests or you have some other way to validate the code. However, in _most_ cases, you'll want to be able to actually test your code by _using_ the code. The build scripts will get you sorted. :thumbs-up:

All of the scripts are `.sh` first! If you are running on a windows machine, you need to execute the scripts in a git-bash shell.

First, here is a list of the scripts. Later on there will be a "workflow-first" approach to the same information.

| script                | description                                                                                                                                                                                                                                                                                       |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `/set-packages.sh`    | builds all your local source code to nuget packages, and installs them inside a local nuget package source. The packages will always have version, `0.0.123`. Note: this script _also_ runs the `/templates/build.sh` script by default, but it _does not_ run the `/cli/cli/install.sh` script.  |
| `/cli/cli/install.sh` | builds your local CLI source code and installs it globally.                                                                                                                                                                                                                                       |
| `/templates/build.sh` | builds your local templates and installs them globally.                                                                                                                                                                                                                                           |

#### I wrote a new CLI command, and I want to play with it

there are _many_ ways to do this, (see the CLI section), but using scripts, you should run the `cli/cli/install.sh` script. This will build your local source code into a CLI, and install it globally. After you do this, you can run `beam` wherever on your machine.

#### I changed some Microservice Framework code, and I want to validate it works in a new SAM project (without using Docker)

Good work, sir/madam for your desire to test your work! If your change is _just_ inside the Microservice Framework, then you should run the `./set-packages.sh` script to bundle all of your Nuget packages to a locally available `0.0.123` version. Once this is done, in your SAM project, you should change the nuget reference from whatever it _was_, to `0.0.123`.

```xml
<PackageReference Include="Beamable.Microservice.Runtime" Version="0.0.123" /> 
```

You may need to run a `dotnet restore` to make the SAM pay attention to the new version of the code. Now, when you start the SAM, it will use your local version of the source code.

There is a more robust way to accomplish this task as well, but with its robustness, comes annoyance. _Technically_, instead of getting all the `Beamable.Microservice.Runtime` code from the nuget package, your SAM project _could_ use `<ProjectReference>`'s to map directly to your source code. This will take longer to set up, but has a nice pay-off. If you spend the time to configure all these source code links, then you don't need to re-run `./set-packages.sh` every time you make a change and want to re-test. (I would encourage you to write a unit test to help with fast iteration speed, but hey, no one scrambles an egg alike).

As an example, Imagine (please) that I have a SAM project in a sibling `/myProject` directory to the monorepo.

```
/myProject
 /services
  /Tuna
/BeamableProduct
 /cli
 /microservice
 etc...
```

Then the `/myProject/services/Tuna/Tuna.csproj` may have references like this,

```xml
<ProjectReference Include="..\..\..\BeamableProduct\microservice\microservice\microservice.csproj" />
```

However, this _won't_ actually work, because the Nuget package reference also carries magical msbuild extensions that we aren't getting. In order to reference the `.props` and `.target` files correctly, we need to add references to the `.props` file at the top of our `.csproj`, and the `.targets` file at the bottom of the `.csproj`.

```xml
<!-- Import the .props file!  -->
<ImportGroup >  
    <Import Project="..\..\..\BeamableProduct\microservice\microservice\Targets\Beamable.Microservice.Runtime.props" />  
</ImportGroup>
```

And then at the bottom,

```xml
<!-- Import the .props file!  -->
<ImportGroup >  
    <Import Project="..\..\..\BeamableProduct\microservice\microservice\Targets\Beamable.Microservice.Runtime.targets" />  
</ImportGroup>
```

**NOTE**: neither of these approaches will work if you need to run the SAM in Docker, because Docker won't have access to these relative paths.

#### I changed some Microservice Framework code, and I want to validate it works in a new SAM project (Inside Docker)

Oh boy, you're in the thick of it, now. The docker use-case requires that all the moving pieces of our software stack line up. Everything needs to be accessible to the docker build step, which means source-code needs to be built and included locally, or the code needs to be built and be available on nuget.org.

Ultimately, what needs to happen is that docker needs to be able to find your source code. There are lots of ways that _could_ happen, but the proposed solution is to have the dockerfile for your SAM project copy in a local Nuget package source, register it, and then find the source code from that.

When you run `./set-packages.sh`, you can pass an argument which is the path to your local `.beamable` project. Then, the output `0.0.123` packages will be sent to that package source. You can write the dockerfile manually, but if hindsight is 20/20, then it would be better to create the service with the `--beamable-dev` flag. This will create a slightly different dockerfile for your service that you can find in the `templates/BeamStorage/Dockerfile-BeamableDev` file.

```Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine as build-env  
WORKDIR /BeamableSource  
COPY ./BeamableSource/*.nupkg ./  
WORKDIR /subsrc/  
RUN dotnet nuget add source /BeamableSource
```

This process is a pain.

## The CLI Solution

The Beam CLI is a dotnet solution located in the `/cli` folder. Confusingly, there is also a dotnet _project_ in `/cli/cli` which is the actual executable CLI project. This CLI project is the dotnet tool we publish to Nuget, [https://www.nuget.org/packages/Beamable.Tools](https://www.nuget.org/packages/Beamable.Tools)

Within the `/cli` solution, there are also a few other project folders,

| path                          | note                                                                                                                     |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| `/cli/beamable.common`        | this is shared code between Unity, the CLI, and the Microservice Framework. Any edits to this project are wide-reaching. |
| `/cli/beamable.server.common` | this is shared code between Unity, the CLI, and the Microservice Framework. Any edits to this project are wide-reaching. |
| `/cli/tests`                  | this is a unit test project for the CLI executable.                                                                      |

The CLI solution also references some code in the `/microservice` folder, specifically,

- `/microservice/beamable.tooling.common`
- `/microservice/unityEngine`
- `/microservice/unityEngine.addressables

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

## Unity SDK

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

## Templates

The `/templates` folder has a solution of dotnet project templates. These templates are built and released as a Nuget package, [https://www.nuget.org/packages/Beamable.Templates/](https://www.nuget.org/packages/Beamable.Templates/). These templates are important for the CLI & Standalone Microservice workflow. When you create a new Standalone Microservice, one of the steps is to run the `dotnet new` command and pass in a template option for these deployed Beamable templates.

I'd recommend reading up on Dotnet Templates, [https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates).

The structure of the templates solution is a bit confusing. There are a set of _actual_ template projects, at the time of this writing,

1. `Beamservice`
2. `BeamStorage`
3. `CommonLibrary`
But there is a very important additional project, `templates`. The `templates` project is the actual project file that is converted into a nuget package. If you inspect the `.csproj` , you'll see this,

```xml
<ItemGroup>  
    <Content Include="Data/BeamService/**" Exclude="Data/BeamService/bin/**;Data/BeamService/obj/**;" />  
    <Compile Remove="**/*" />  
</ItemGroup>  
<ItemGroup>  
    <Content Include="Data/BeamStorage/**" Exclude="Data/BeamStorage/bin/**;Data/BeamStorage/obj/**;" />  
    <Compile Remove="**/*" />  
</ItemGroup>  
<ItemGroup>  
    <Content Include="Data/CommonLibrary/**" Exclude="Data/CommonLibrary/bin/**;Data/CommonLibrary/obj/**;" />  
    <Compile Remove="**/*" />  
</ItemGroup>
```

The goal of this configuration is to copy in files from the `/Data` folder into the final nuget package. This allows the single `template` project to carry multiple project templates, instead of having a single nuget package per template project.

Admittedly, this is confusing. there is a `build.sh` script in the templates solution that will copy the files from the various projects into the `/templates/Data`, and then run the nuget pack. The `build.sh` script will install the templates to your local computer, so that you can use them.

## Microservice Framework

The Microservice framework code is inside the `/microservice` solution. There is a `/microservice/microservice` project which is the Dotnet project that gets packed into the nuget package, `Beamable.Microservice.Runtime`.[https://www.nuget.org/packages/Beamable.Microservice.Runtime/](https://www.nuget.org/packages/Beamable.Microservice.Runtime/)

The `microservice` solution has several projects referenced,

| path                                          | note                                                                                                                                                                      |
| --------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `/microservice/beamable.tooling.common`       | this is shared code between the CLI and the Microservices. Really, this is shared beamable code that cannot be put in `cli/beamable.common` due to the Unity requirement. |
| `/microservice/unityEngineStubs`              | this is a proxy library for basic unity types                                                                                                                             |
| `/microservice/unityEngineStubs.addressables` | this is a proxy library for basic addressable types. It must exist is as a separate project due to how assemblies are loaded in Unity itself.                             |
| `/microservice/microserviceTests`             | unit and smoke tests for the microservice framework                                                                                                                       |
| `/microservice/microserivce`                  | the actual framework code.                                                                                                                                                |

The `/cli/beamable.common` and `/cli/beamable.server.common` projects are also _referenced_ through the solution file.

An unusual part of the packaging of the `microservice/microservice` project into the Nuget package, `Beamable.Microservice.Runtime`, is that not _all_ of the project references are deployed as Nuget packages themselves. Specifically, `beamable.tooling.common` is not currently a Nuget package. This means that `Beamable.Microservice.Runtime` needs to manually include the dependency as a built dll, and the sub-dependencies of that library as well. Its a pain, and I hope we change this some day soon.

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
