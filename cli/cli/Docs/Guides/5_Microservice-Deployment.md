Deploying Beamable Standalone Microservices


## Dependencies

Before you can deploy Beamable Standalone Microservices, you need to complete the [Getting-Started Guide](doc:cli-guide-getting-started). That means having [Dotnet 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed, and getting the [Beam CLI](https://www.nuget.org/packages/Beamable.Tools). 

You can confirm you have everything installed checking the versions of the tools.
```sh
dotnet --version
beam version # beam --version also works.
```

To deploy, you also need to have [Docker](https://www.docker.com/products/docker-desktop/) installed. You can verify it is installed correctly by using the `--version` command.
```sh
docker --version
```

In order to deploy a Microservice, you also need to have a local `.beamable` workspace with a Beamable Standalone Microservice. As a reminder, you can create one quickly using the commands below.
```sh
beam init MyProject
cd MyProject
dotnet beam project new service HelloWorld
```

## Deployment

Beamable Standalone Microservices use Dotnet as the core technology to run locally on your development machine. However, when it is time to release your Microservice to production, the service will be built into an OCI compliant image using [Docker](https://www.docker.com/products/docker-desktop/), and uploaded to the Beamable Cloud. Beamable's internal orchestration platform (sometimes called _BeamO_ ) will deploy containerized instances of the service. 

To deploy your local project, use the [services deploy](doc:cli-services-deploy) command.
```sh
dotnet beam services deploy
```

This will validate that your Standalone Microservice can be built, and that it will start accepting HTTPS traffic. Each Standalone Microservice will be built into a Docker image and then run locally as a Docker container. The `deploy` command will wait for the service to start responding to health check API calls. After the service responds as healthy, the `deploy` command will upload the Docker image to Beamable. 

![Uploading a microservice](https://files.readme.io/4a8f62b-image.png)


> 📘 Docker CPU Architecture
>
> Beamable Cloud requires that all services are built to x64 CPU architectures. Unfortunately, this means that developers with non x86 based computers will need to _cross compile_ services, which causes the `deploy` process to take longer. 

After all the Microservices have uploaded, the `deploy` command commits a _service manifest_ to the Beamable API, which creates Beamable Cloud resources for your services. The services will be enabled, and you can see them available in the Beamable Portal's _Operation/Microservices_ section.

![Beamable Deployments](https://files.readme.io/55ee363-image.png)

Now that your service is running on the Beamable Cloud, you can send HTTPS traffic to your service. To help test, you can open the dot-dot-dot menu on the right side of the `HelloWorld`
 card, and select "Docs" to open an Open API page.

## Disabling Services

After a Standalone Microservice has been deployed, it will continue to be available on the Beamable Cloud until it is disabled. A service can be temporarily disabled through Portal, but they will be re-activated after a fresh `deploy` command occurs. 

To disable a service through a `deploy` command, you need to modify the configuration of the Microservice source code itself. In the `.csproj` file of your service, set the `<BeamEnabled>` property to `false`. Then, re-run the `deploy` command. See the [Microservice Configuration Section](doc:cli-guide-microservice-configuration) for more details about project configuration. 

> 📘 Services Cost Money! 
>
> Remember, Every service running on Beamable Cloud may increase your Beamable Bill. Disable your services to reduce your monthly bill.

## Dockerfiles

Standalone Microservices build custom Docker images that are run on the Beamable Cloud. Each service has a `Dockerfile` that defines how the Docker image is constructed. 

You may modify this file to extend the capabilities of your resulting docker image. However, there are a few restrictions about how you may modify the file. Do not edit anything between the following tags, 
- `<BEAM-CLI-COPY-ENV>`
- `<BEAM-CLI-COPY-SRC>` 

Those tags define special places in the `Dockerfile` where the Beam CLI will inject custom data about your project structure. Any changes you make between these tags will be overwritten every time the Beam CLI runs.  

You may change the dotnet framework version at your own peril. Beamable has validated that dotnet 6 is stable for all supported platforms. However, versions after dotnet 6 may not cross compile CPU architectures correctly. Beamable requires that Docker images are built to x86 architecture. If your developer team has members with non x86 CPU based computers (like a recent Apple Silicon Mac), then those developers will not be able to upload to Beamable Cloud.

Finally, the port `6565` **MUST** be exposed. The port is used as a health check mechanism within the Beamable Cloud. If you delete that line, your services will fail to pass health checks and cannot be uploaded. 

Here is a sample `Dockerfile` from a service created with Beam CLI 2.1.0.

```Dockerfile
# use the dotnet sdk as a build stage  
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine as build-env  
ARG TARGETARCH  
  
# <BEAM-CLI-COPY-ENV> this line signals the start of environment variables copies into the built container. Do not remove it. This will be overwritten every time a variable changes in the execution of the CLI.  
  
ENV BEAM_CSPROJ_PATH="/subsrc/services/Example4/Example4.csproj"  
ENV BEAM_VERSION="2.1.0"  
# </BEAM-CLI-COPY-ENV> this line signals the end of environment variables copies into the built container. Do not remove it.  
  
# <BEAM-CLI-COPY-SRC> this line signals the start of Beamable Project Src copies into the built container. Do not remove it. The content between here and the closing tag will change anytime the Beam CLI modifies dependencies.  
  
RUN mkdir -p /subsrc/services/Data  
COPY services/Data /subsrc/services/Data  
  
RUN mkdir -p /subsrc/services/Example4  
COPY services/Example4 /subsrc/services/Example4  
# </BEAM-CLI-COPY-SRC> this line signals the end of Beamable Project Src copies. Do not remove it.  
  
WORKDIR /  
  
# build the dotnet program  
RUN dotnet publish ${BEAM_CSPROJ_PATH} -p:BeamableVersion=${BEAM_VERSION} -a $TARGETARCH -c release -o /beamApp  
  
# use the dotnet runtime as the final stage  
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine  
WORKDIR /beamApp  
  
# expose the health port  
EXPOSE 6565   
# copy the built program  
COPY --from=build-env /beamApp .  
  
# when starting the container, run dotnet with the built dll  
ENTRYPOINT ["dotnet", "/beamApp/Example4.dll"]  
  
# Swap entrypoints if the container is exploding and you want to keep it alive indefinitely so you can go look into it.  
#ENTRYPOINT ["tail", "-f", "/dev/null"]
```


## Local Docker Testing

The recommended developer workflow is to run your micro services using Dotnet. However, because deployments are running within a container, it may be beneficial to validate that your services work in Docker before deploying them. 

The [services run](doc:cli-services-run) command will run your services on your local machine with Docker. It will build the `Dockerfile` into an image, and run a container for you. Remember that the [project run](doc:cli-project-run) command runs services locally as dotnet processes, where-as this command will start a Docker container. 

```sh
dotnet beam services run --ids HelloWorld
```

You can validate that your services are running in docker using [project ps](doc:cli-project-ps) or by using Docker directly.

```sh
dotnet beam project ps 
docker ps
```

After you are done testing, you can use Docker directly to stop all the containers, or your can use [services stop](doc:cli-services-stop) command. 

```sh
dotnet beam services stop
```