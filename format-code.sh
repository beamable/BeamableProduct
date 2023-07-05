#!/bin/bash

dotnet tool restore
dotnet tool run dotnet-format -f ${1:"client/Packages"}
