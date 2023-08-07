## Regular flow for updating 

By default, in order to update dotnet cli tool it is normal to use command like:

```
dotnet tool update {name_of_tool} --global --version {version_here}
```

Which for this tool could be something like:

```
dotnet tool update Beamable.Tools --global --version 1.17.0
```

### Simpler way

In cli tool, as of 1.16.2, it is possible to use `beam version` tools to get the list of available versions:

```
beam version ls
```

The `beam version ls` command can also have arguments like  `--include-rc {true/false}` or `--include-release {true/false}` so it is possible to specify if it should print out RC versions, release ones or both of them.

Then in order to install specific version you can use the `beam version` command:

```
beam version install <version?>
```

That command will install the specified version OR the latest one if no version as argument is provided.
