#!/bin/bash

revision=$(git rev-parse HEAD)
outputDirPath=$1/revision.txt
echo $revision > $outputDirPath
