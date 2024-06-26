#!/bin/sh

BASEDIR=$(dirname "$0")
echo "$BASEDIR"

TargetEngine="$1"

# For unity, this defaults to /client, but can be overridable 
PathToWorkingDirectory="$2"
if [[  $TargetEngine == "UNITY" && $PathToWorkingDirectory == "" ]]; then
    PathToWorkingDirectory="$BASEDIR/client"
fi
PathToWorkingDirectory="${PathToWorkingDirectory//\\/\/}"

# For unreal
PathToRestoreDirectory="$3"
if [[  $TargetEngine == "UNREAL" && $PathToRestoreDirectory == "" ]]; then
    PathToRestoreDirectory="$PathToWorkingDirectory"Microservices
fi

# This argument is the path to GitBash 99% of the time. If not given, assumes C:/Program Files/Git/bin/bash.exe
PathToUnixShell="$3"
if [[ $PathToUnixShell == "" ]]; then
  case "$OSTYPE" in
    solaris*) echo "/bin/bash" ;;
    darwin*)  echo "/bin/bash" ;; 
    linux*)   echo "/usr/bin/bash" ;;
    bsd*)     echo "/usr/bin/bash" ;;
    msys*)    PathToUnixShell="C:\Program Files\Git\bin\bash.exe" ;;
    cygwin*)  PathToUnixShell="C:\Program Files\Git\bin\bash.exe" ;;
    *)        echo "Should never see this!!" ;;
  esac
  
fi
PathToUnixShell="${PathToUnixShell//\\/\/}"
  
# Path to the CLI .run folder.
CliRunPath="$BASEDIR/cli/.run"
  
if [ -d "$CliRunPath" ]; then
  echo "Copying all TEMPLATE- configurations into $TargetEngine- configurations."
  
  for i in $(find "$CliRunPath" -name 'TEMPLATE-*.run.xml'); do # Not recommended, will break on whitespace  
    
    TargetFile="${i/TEMPLATE-/"$TargetEngine"-}"
    echo "cp -u $i $TargetFile"
    cp -u "$i" "$TargetFile"
    echo sed -i 's/TEMPLATE-/'"$TargetEngine"'-/g' "$TargetFile"
         sed -i 's/TEMPLATE-/'"$TargetEngine"'-/g' "$TargetFile"

    # If there is an INTERPRETER_PATH in the xml, replace it with the given $PathToUnixShell
    echo sed -i '\@="INTERPRETER_PATH"@s@value=".*"@value="'"$PathToUnixShell"'"@g' "$TargetFile"
         sed -i '\@="INTERPRETER_PATH"@s@value=".*"@value="'"$PathToUnixShell"'"@g' "$TargetFile"
         
    # For the local packages script we also change the SCRIPT_OPTIONS to point to the $PathToWorkingDirectory
    # For every other script, we set the WORKING_DIRECTORY path in the xml, replace it with the given $PathToWorkingDirectory
    if [[ $TargetFile == *$TargetEngine-Set-Local-Packages* || $TargetFile == *$TargetEngine-Set-Install-* ]]; then
      echo sed -i '\@="SCRIPT_OPTIONS"@s@\$PROJECT_DIR\$\/\.\.\/client@'"$PathToWorkingDirectory"'@g' "$TargetFile"
           sed -i '\@="SCRIPT_OPTIONS"@s@\$PROJECT_DIR\$\/\.\.\/client@'"$PathToWorkingDirectory"'@g' "$TargetFile"
           
      echo sed -i '\@="SCRIPT_OPTIONS"@s@BeamableNugetSource@'"$TargetEngine"'_NugetSource@g' "$TargetFile"
           sed -i '\@="SCRIPT_OPTIONS"@s@BeamableNugetSource@'"$TargetEngine"'_NugetSource@g' "$TargetFile"

      echo sed -i '\@="SCRIPT_OPTIONS"@s@PathToRestore@'"$PathToRestoreDirectory"'@g' "$TargetFile"
           sed -i '\@="SCRIPT_OPTIONS"@s@PathToRestore@'"$PathToRestoreDirectory"'@g' "$TargetFile"
                                 
      continue
    else          
      echo sed -i '\@="WORKING_DIRECTORY"@s@value=".*"@value="'"$PathToWorkingDirectory"'"@g' "$TargetFile"
           sed -i '\@="WORKING_DIRECTORY"@s@value=".*"@value="'"$PathToWorkingDirectory"'"@g' "$TargetFile"
    fi
  done  

else
  echo "Invalid path to a .run folder. Given Path: $CliRunPath"
fi   







