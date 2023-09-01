#!/bin/bash
set -eu
IFS=$'\n\t'

# Use uppercase, with no hyphens or spaces
PROJECT_NAME_UPPER="RLE"
FUNCTION_CODE="INF"

# CSV list of any additional trusted CA certificate roots thumbprints
# Palo Alto CSC Root CA, and *.defra.azure.cloud CA
TRUSTED_ROOT_THUMBPRINTS="BDB406F8EB50F8A56FCC8B1F95624B84A8A5E607,A8985D3A65E5E5C4B2D7D66D40C6DD2FB19C5436"

# Copy to environment vars
echo "PROJECT_NAME_UPPER=$PROJECT_NAME_UPPER" >> $GITHUB_ENV
echo "FUNCTION_CODE=$FUNCTION_CODE" >> $GITHUB_ENV
echo "TRUSTED_ROOT_THUMBPRINTS=$TRUSTED_ROOT_THUMBPRINTS" >> $GITHUB_ENV
