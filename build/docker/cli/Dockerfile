FROM mcr.microsoft.com/dotnet/sdk:8.0

# RUN npm install -g json
ADD build/docker/scripts /scripts

WORKDIR /Client
ENTRYPOINT sh /scripts/build-cli.sh