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
someOtherFolder
```

| call directory     | which config would be used         |
| ------------------ | ---------------------------------- |
| `mainFolder`       | `mainFolder/.beamable`             |
| `childFolder`      | `mainFolder/childFolder/.beamable` |
| `yetAnotherFolder` | `mainFolder/childFolder/.beamable` |
| `someOtherFolder`  | no config is available             |


## Validation 

From any folder, you can run the [beam config](doc:cli-config)  command to print information about your current Beamable folder. 

In the example directory structure above, if the `beam config` command was invoked from the `mainFolder`, it would log information about the `mainFolder/.beamable` folder. 
```sh
mainFolder % beam config
 {                                                             
    "host": "https://api.beamable.com",                        
    "cid": "<redacted>",                                 
    "pid": "<redacted>",                              
    "configPath": "/Users/examples/mainFolder/.beamable" 
 } 
```

However, if the `beam config` command was invoked from the `someOtherFolder` path, you should expect to see an error, because there is no `.beamable` folder within the parent linear. 

```sh
someOtherFolder % beam config

**Error** [0404]: Could not find any .beamable config folder which is required for this command.

NOTE: Consider calling `beam init` first.

  

Logs at

  /var/folders/ys/949qmfy15r7bl8x36s6wmm000000gn/T/beamCliLog.txt
```

## Folder structure

The one file that always will and should be is `connection-configuration.json` containing info about host, CID and PID of current configuration.
The rest of the files are described in the table below:

| path                        |                     description                      | can be included in VCS |
| --------------------------- | :--------------------------------------------------: | ---------------------: |
| `.gitignore`                | Default rules what should not be included in git VCS |                    yes |
| `temp/connection-auth.json` |        File containing local user credentials        |                     no |
| `localTags.json`            |        File containing tags of local content         |                    yes |
| `Content/*json`             |            Files describing each content             |                    yes |

