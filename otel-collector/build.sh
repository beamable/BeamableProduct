#!/bin/bash

platforms=("windows/amd64" "windows/arm64" "linux/amd64" "linux/arm64" "darwin/amd64" "darwin/arm64")


echo "Deleting old collector binaries..."
rm ../cli/cli/Resources/*

cd beamable-collector/

for platform in "${platforms[@]}"; do
    OS=${platform%/*}
    ARCH=${platform#*/}
    output="collector-$OS-$ARCH"
    
    if [ "$OS" == "windows" ]; then
        output+=".exe"
    fi
    
    echo "Building Otel Collector for $OS/$ARCH..."
    GOOS=$OS GOARCH=$ARCH go build -o ../../cli/cli/Resources/$output
done

cd ..

echo "Copying config file to Resources folder..."
cp clickhouse-config.yaml ../cli/cli/Resources/

echo "Collector build process completed!"