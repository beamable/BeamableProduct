#!/bin/bash

default_dir='client/Packages'
dir=${1:-$default_dir}

dotnet tool restore
dotnet tool run dotnet-format -f $dir
