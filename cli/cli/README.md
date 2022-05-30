# Beam CLI

## Development
You'll need dotnet 6.0.1 installed. Open up the top level `./cli/cli.sln` file.

## Build
Inside the `./cli/cli` folder, Run
```shell
dotnet pack
```

Then uninstall an existing tool with
```shell
dotnet tool uninstall cli -g
```

and re-install the new tool with
```shell
dotnet tool install --global --add-source ./nupkg/ cli
```

Alternatively, run the `./install.sh` file to do all 3 steps. 

## Usage
After building, you can use `beam` on your CLI.
See `beam --help` for a list of commands. 