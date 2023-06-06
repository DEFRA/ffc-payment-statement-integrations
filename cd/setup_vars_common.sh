#!/bin/bash
set -eu
IFS=$'\n\t'

# Creating a build manifest to pass variables to later steps in the pipeline
export PROJECT_NAME="logicappstda"
export ARTIFACT_NAME="deploy_artifacts"
# API Connections - if you change this name, you must change the reference in workflow.json
export BLOB_CONNECTION_NAME="azureblob"

# Copy to environment vars
echo "PROJECT_NAME=$PROJECT_NAME" >> $GITHUB_ENV
echo "ARTIFACT_NAME=$ARTIFACT_NAME" >> $GITHUB_ENV
echo "BLOB_CONNECTION_NAME=$BLOB_CONNECTION_NAME" >> $GITHUB_ENV
