## Requirements and Assumptions
This project requires the following tools to be installed.
1. [Dotnet (6.0 or later)](https://dotnet.microsoft.com/en-us/download)
2. [Docker](https://www.docker.com/products/docker-desktop/)

This project assumes that you are using [Git](https://git-scm.com/) for version control. 
A `.gitignore` file is included, but if you are using a different version control system, you 
must create a custom ignore-file.

## Getting Started

Before you begin, you'll need to create a Beamable organization.
You can create a Beamable organization through the Unity Editor, or online at [Beamable.com](https://beta-portal.beamable.com/signup/registration/).

You will need to install the Beamable dotnet templates. 
These templates are available on Nuget.
```shell
# install the template
dotnet new -i Beamable.Templates
```

Now that the templates are installed, you can create a new dotnet project using the beamservice template.
```shell
# create a project
dotnet new beamservice -n MyService
# navigate your terminal into the project
cd MyService
```

Now that you have a project, you'll need to run a command to update the template for your Beamable organization.
This command will update the Beamable SDK and Tools package to the latest version, prompt you for 
your Beamable login credentials, and configure the project with Docker.

```shell
# initialize the Beamable project
dotnet build -t:beam-init
```


## FAQ

#### How do I run the service?
```shell

# give it a name, Xyz
# path to docker build context
# path to docker file
dotnet beam services register

# pass in ids
dotnet beam services deploy 

```

#### What is the /Src folder?
Any file that is not included in the `/src` folder will not be included in the built service.
Build time configurations and documentation may exist outside the `/src` folder, but everything
else should exist in the `/src`, otherwise it won't be used. 

#### How do I publish the service?

#### How do I configure the service?

#### How do I update Beamable?
Update the version number in `./config/dotnet-tools.json`
Update the version number in `./BeamService.csproj`
