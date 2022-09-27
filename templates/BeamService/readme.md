## Requirements
1. [Dotnet (6.0 or later)](https://dotnet.microsoft.com/en-us/download)
2. [Docker](https://www.docker.com/products/docker-desktop/)

## Getting Started

In order to install the template
```shell
# install the template
dotnet new -i Beamable.Templates

# create a project
dotnet new beamservice -n MyService
```
When you have a project, make sure to update Beamable to the latest version before getting started.
```shell
# update to the latest version of beamable
dotnet build -t:updatebeamable
```

## FAQ

#### How do I run the service?

#### How do I publish the service?

#### How do I configure the service?

#### How do I update Beamable?
Update the version number in `./config/dotnet-tools.json`
Update the version number in `./BeamService.csproj`
