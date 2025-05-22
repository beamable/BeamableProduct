#!/bin/bash

platforms=("windows/amd64" "windows/arm64" "linux/amd64" "linux/arm64" "darwin/amd64" "darwin/arm64")


# chatGPT wrote this; the goal is emulate C#'s Environment.SpecialFolder.LocalApplicationData
get_app_data_dir() {
  unameOut="$(uname -s)"
  case "${unameOut}" in
      Linux*)     echo "$HOME/.config";;
      Darwin*)    echo "$HOME/Library/Application Support";;
      CYGWIN*|MINGW*|MSYS*) 
                  # Windows Git Bash or similar
                  echo "$APPDATA";;
      *)          echo "$HOME/.config";;
  esac
}


echo "Deleting old collector binaries..."
OUTPUT_DIRECTORY="${OUTPUT_DIRECTORY:-$(get_app_data_dir)/beam/collectors/0.0.123}"
rm -rf $OUTPUT_DIRECTORY
mkdir -p "$OUTPUT_DIRECTORY"
echo "Output:${OUTPUT_DIRECTORY}"



cd beamable-collector/

for platform in "${platforms[@]}"; do
    OS=${platform%/*}
    ARCH=${platform#*/}
    output="collector-$OS-$ARCH"
    
    if [ "$OS" == "windows" ]; then
        output+=".exe"
    fi
    
    echo "Building Otel Collector for $OS/$ARCH..."
    GOOS=$OS GOARCH=$ARCH go build -o "$OUTPUT_DIRECTORY/$output"
done

cd ..

echo "Copying config file to Resources folder..."
cp clickhouse-config.yaml "$OUTPUT_DIRECTORY"

echo "Collector build process completed!"