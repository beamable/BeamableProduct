Get up and running with the Beamable CLI.

The Beamable CLI is a dotnet tool that allows developers to interact with Beamable. It can manage a variety of Beamable technologies, including Microservices, Content, and other services. 

## Dependencies
You'll need to install [Dotnet 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) before you can get started. 
Verify it is installed by running `dotnet --version` from a terminal.

## Installing

To install the Beamable CLI, run the following commands in a shell.

```shell
dotnet tool install --global Beamable.Tools
```

And verify your installation with `beam version`.

### Updating
As of 1.16.2, a globally installed CLI can manage its own updates through the use of the [beam version install](doc:cli-version-install) command.

The following command will install the latest CLI. The "latest" string can be any valid CLI version
```shell
beam version install latest
```

> 📘 Check Versions on Nuget
>
> Remember, Beamable.Tools is a dotnet tool available through Nuget. As such, you can find all available versions at [nuget.org](https://www.nuget.org/packages/Beamable.Tools) 


## Getting Started

Now that Beamable is installed, you can connect to an existing Beamable organization. If you haven't setup an organization yet, [create a Beamable organization](https://beta-portal.beamable.com/signup/registration/) first. 

You can connect the CLI to your Beamable organization with the [beam init](doc:cli-init) command. 

```shell
mkdir MyProject
beam init
```

This command will prompt you for your organization's alias, your credentials, and which realm to use. When it is complete, you should see a `./beamable` folder in the current directory. See the [Configuration](doc:cli-configuration) for details about this folder. Now, you can run a [beam config](doc:cli-config) command to verify your project is set up.

```shell
beam config
```
You should expect to see your CID/PID printed out. 

To check that everything is working correctly, you can use the [beam me](doc:cli-me) command. Now you have a configured CLI project! 

### Next Steps

From here, you can
- setup [Standalone Microservices](doc:cli-microservices)
- manage Content, or
- listen to server events