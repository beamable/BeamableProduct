What is the .beamable folder?

The Beam CLI uses a `./beamable` folder to manage state between multiple invocations of `beam` commands. The `./beamable` folder has information for 1 Beamable project. Everytime you execute a `beam` command, it searches for the nearest `./beamable` folder in the parent lineage of your current directory. If you run [beam config](doc:cli-config) in a folder containing `./beamable`, or any child folder, then that `./beamable` folder is used for the configuration.

Wip