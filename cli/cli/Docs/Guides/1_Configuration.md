## What is the .beamable folder?

The Beam CLI uses a `./beamable` folder to manage state between multiple invocations of `beam` commands. 
The `./beamable` folder has information for 1 Beamable project.
Everytime you execute a `beam` command, it searches for the nearest `./beamable` folder in the parent lineage of your current directory.
If you run [beam config](doc:cli-config) in a folder containing `./beamable`, or any child folder,
then that `./beamable` folder is used for the configuration. 

We can also pass `--dir <directory_path>` flag to `beam` command in order to specify specific directory to use as config directory.

### Example

With given structure:
```
mainFolder
├───.beamable
└───childFolder
    ├───.beamable
    └───yetAnotherFolder
```

| call directory     | which config would be used         |
|--------------------|------------------------------------|
| `mainFolder`       | `mainFolder/.beamable`             |
| `childFolder`      | `mainFolder/childFolder/.beamable` |
| `yetAnotherFolder` | `mainFolder/childFolder/.beamable` |


## Folder structure

The one file that always will and should be is `config-defaults.json` containing info about host, CID and PID of current configuration.
The rest of the files are described in the table below:

| path                      |                     description                      | can be included in VCS |
|---------------------------|:----------------------------------------------------:|-----------------------:|
| `.gitignore`              | Default rules what should not be included in git VCS |                    yes |
| `user-token.json`         |        File containing local user credentials        |                     no |
| `beamoLocalManifest.json` |  Config representation of Beamo Service Definitions  |                    yes |
| `beamoLocalRuntime.json`  |   Description of existing local service instances    |                     no |
| `localTags.json`          |        File containing tags of local content         |                    yes |
| `Content/*json`           |            Files describing each content             |                    yes |

Soon there should be option to generate ignore files for at least most popular VCS using `beam vcs generate-ignore-file --type git|p4|svn` command.