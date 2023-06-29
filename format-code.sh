#!/bin/bash

dotnet tool restore
dotnet-format -f ${1:"client/Packages"}
