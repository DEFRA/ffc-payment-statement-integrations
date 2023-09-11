#!/bin/bash
set -eu
IFS=$'\n\t'

# Use uppercase, with no hyphens or spaces
PROJECT_NAME_UPPER="RLE"
FUNCTION_CODE="INF"

# Copy to environment vars
echo "PROJECT_NAME_UPPER=$PROJECT_NAME_UPPER" >> $GITHUB_ENV
echo "FUNCTION_CODE=$FUNCTION_CODE" >> $GITHUB_ENV
