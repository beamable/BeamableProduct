Get up and running with the Beamable CLI.

The Beamable CLI is a dotnet tool that allows developers to interact with Beamable. It can manage a variety of Beamable technologies, including Microservices, Content, and other services. 

## Dependencies
You'll need to install [Dotnet 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) before you can get started. 
Verify it is installed by running `dotnet --version` from a terminal.

## Installing

To install the Beamable CLI, run the following command in a shell.

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

There may be updates you are required to do, so please check the [migration guide](doc:cli-guide-upgrading).

## Getting Started

Now that Beamable is installed, you can connect to an existing Beamable organization. If you haven't setup an organization yet, [create a Beamable organization](https://beta-portal.beamable.com/signup/registration/) first. 

You can connect the CLI to your Beamable organization with the [beam init](doc:cli-init) command. 

```shell
mkdir MyProject
beam init
```

This command will prompt you for your organization's alias, your credentials, and which realm to use. When it is complete, you should see a `./beamable` folder in the current directory. See the [Configuration](doc:cli-guide-configuration) for details about this folder. Now, you can run a [beam config](doc:cli-config) command to verify your project is set up.

```shell
dotnet beam config
```
You should expect to see your CID/PID printed out. 

As of CLI 2.1.0, anytime you create a Beamable workspace, the CLI will be installed as a local tool next to the workspace's `.beamable` folder. This means that you can run the local tool with `dotnet beam`. If you continue to use `beam` in the workspace, the global installation will automatically forward your command to the local tool. This will be inefficient and lead to poor performance. We recommend you use `dotnet beam` wherever possible. 

To check that everything is working correctly, you can use the [beam me](doc:cli-me) command. Now you have a configured CLI project! 

> 📘 Finding Help
>
> You can pass the `--help` flag to any command to print out detailed information about the arguments and options for the given command. Also, the `--help-all` flag will include additional information used by internal Beamable developers. You are welcome to use the internal facing commands, but they are not officially supported. 

### Next Steps

From here, you can
- setup [Standalone Microservices](doc:cli-guide-microservices)
- manage Content,
- listen to server events,
- [learn how the CLI handles data output](doc:cli-guide-command-line-output).