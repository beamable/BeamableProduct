Get up and running with the Beamable CLI.

The Beamable CLI is a dotnet tool that allows developers to interact with Beamable via the CLI. It can be used to manage Beamable Content, Microservices, as well as make arbitrary requests to the Beamable backend. 

## Dependencies
You'll need to install [Dotnet 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) before you can get started. 
Verify it is installed by running `dotnet --version` from a terminal.

## Installing

To install the Beamable CLI, run the following commands in a shell.

```shell
dotnet tool install --global Beamable.Tools
```

And verify your installation with `beam --version`. 

## Getting Started

Now that Beamable is installed, you can either create a new Beamable organization with the [beam org new](doc:cli-org-new) command, or connect to an existing one with the [beam init](doc:cli-init) command.

If you have an existing organization, you should use [beam init](doc:cli-init).

```shell
beam init --save-to-file
```

> 🚧 Authentication!
> 
> Don't forget the `--save-to-file` option! By default, the CLI requires you pass authorization for each command you execute. If you don't want to do this, the `--save-to-file` option will save your authorization information to the `./beamable` folder and reuse it for later requests. If you forgot to use this option, you can use the [beam login](doc:cli-login) command later.

This command will prompt you for your organization's alias, your credentials, and which realm to use. When it is complete, you should see a `./beamable` folder in the current directory. See the [Configuration](doc:cli-configuration) for details about this folder. Now, you can run a [beam config](doc:cli-config) command to verify your project is set up.

```shell
beam config
```
You should expect to see your CID/PID printed out. 

To check that everything is working correctly, you can use the [beam me](doc:cli-me) command. Now you have a configured CLI project! 