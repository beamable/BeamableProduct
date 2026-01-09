# Use a base image with dependencies
ARG GAME_CI_UNITY_EDITOR_IMAGE
FROM $GAME_CI_UNITY_EDITOR_IMAGE

# Install dependencies
RUN apt-get update && apt-get install -y \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Set .NET version
ENV DOTNET_VERSION=8.0.302

# Download and install .NET SDK
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version $DOTNET_VERSION --install-dir /usr/share/dotnet

# Set environment variables for global availability
ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH="${DOTNET_ROOT}:${PATH}"

# Verify installation
RUN dotnet --info