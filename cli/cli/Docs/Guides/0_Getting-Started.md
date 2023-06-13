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

Now that Beamable is installed, you can either create a new Beamable organization with the [beam org new](doc:cli-org-new) command,
or connect to an existing one with the [beam init](doc:cli-init) command.