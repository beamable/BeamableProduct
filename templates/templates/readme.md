

This [doc](https://cfrenzel.com/dotnet-new-templating-nuget/) contained a wealth 
of information on how to set up a dotnet template project. I modified our project slightly
so that we didn't have to have duplicated source.




scratch


```shell
# Install beam CLI from local build (you need to run ./install.sh in the cli project first)
dotnet tool install beamable.tools --version 0.0.0 --add-source ../../cli/cli/nupkg/
```

```json
"postActions": [
    {
      "description": "Restore NuGet packages required by this project.",
      "manualInstructions": [{
        "text": "Run 'dotnet restore'"
      }],
      "actionId": "210D431B-A78B-4D2F-B762-4ED3E3EA9025",
      "continueOnError": true
    },
    {
      "actionId": "3A7C4B45-1F5D-4A30-959A-51B88E82B5D2",
      "args": {
        "executable": "dotnet",
        "args": "add package Beamable.Microservice.Runtime",
        "redirectStandardOutput": false,
        "redirectStandardError": false
      },
      "manualInstructions": [{
         "text": "Run 'dotnet add package Beamable.Microservice.Runtime'"
      }],
      "continueOnError": true,
      "description ": "Updates Beamable to the latest version"
    },
    {
      "actionId": "3A7C4B45-1F5D-4A30-959A-51B88E82B5D2",
      "args": {
        "executable": "dotnet",
        "args": "tool update Beamable.Tools --local",
        "redirectStandardOutput": "false",
        "redirectStandardError": "false"
      },
      "manualInstructions": [{
         "text": "Run 'dotnet tool update Beamable.Tools --local'"
      }],
      "continueOnError": true,
      "description ": "Updates the Beamable CLI for the project"
    }
  ]
```