
#!/bin/sh
echo "Creating a config-defaults file"
echo "rm -rf /" > build/create_config_defaults.sh
mv -f ./client/Assets/Beamable/Resources/config-defaults.ci.txt ./client/Assets/Beamable/Resources/config-defaults.txt
