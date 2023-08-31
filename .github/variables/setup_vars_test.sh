#!/bin/bash
set -eu
IFS=$'\n\t'

# MUST BE UPPERCASE
ENV_SHORT_NAME="TST"

RESOURCE_PREFIX="$ENV_SHORT_NAME$PROJECT_NAME_UPPER$FUNCTION_CODE"
LOGIC_APP_NAME="${RESOURCE_PREFIX}LA1402"
#####AZURE_SUBSCRIPTION_ID="d49bcc71-d9a8-400b-9385-0015251d90c5"
RESOURCE_GROUP_LOCATION="uksouth"
RESOURCE_GROUP_NAME="${RESOURCE_PREFIX}RG1401"
APP_SERVICE_PLAN_NAME="${RESOURCE_PREFIX}ASP1401"

# Gets converted to lowercase later in the pipeline. No spaces or hyphens allowed
STORAGE_NAME="${RESOURCE_PREFIX}ST1401"
FILE_SHARE_NAME="${RESOURCE_PREFIX}ST1401fs"

APP_INSIGHTS_NAME="${RESOURCE_PREFIX}AIS1401"
DASHBOARD_NAME="${RESOURCE_PREFIX}DSH1401"
DASHBOARD_DISPLAY_NAME="${LOGIC_APP_NAME} dashboard"

# User-assigned Managed Identity - used to access Key Vault (permissions for this can be pre-assigned before this IaC is run)
USER_MANAGED_IDENTITY_NAME="${RESOURCE_PREFIX}UAMID1401"

# VNET integration is required for Private Endpoint blob storage
SUBNET_RESOURCE_ID="/resourceGroups/TSTRLENETRG1401/providers/Microsoft.Network/virtualNetworks/TSTRLENETVN1401/subnets/TSTRLENETSU1402"
# Subnet resource ID excluding '/subscriptions/<subscriptionId>', for example: /resourceGroups/<RGname>/providers/Microsoft.Network/virtualNetworks/<VnetName>/subnets/subnetName>
DNS_SERVER="10.178.0.4"
DNS_ALT_SERVER="10.178.0.5"

# Connector details for external Service Bus
SERVICEBUS_CONNECTION_NAME_SP="serviceBus"
SERVICEBUS_NAMESPACE_SP="DEVFFCINFSB1001.servicebus.windows.net"

# Details for Azure Alerting when workflow catastrophic fails
ACTION_GROUP_NAME="${RESOURCE_PREFIX}AG1401"
SCHEDULED_QUERY_RULE_NAME="${RESOURCE_PREFIX}AIAR1401"
ALERTING_EMAIL_ADDRESS="jeremy.barnsley@defra.gov.uk"

# Copy to environment vars
echo "ENV_SHORT_NAME=$ENV_SHORT_NAME" >> $GITHUB_ENV
echo "LOGIC_APP_NAME=$LOGIC_APP_NAME" >> $GITHUB_ENV
echo "RESOURCE_GROUP_LOCATION=$RESOURCE_GROUP_LOCATION" >> $GITHUB_ENV
echo "RESOURCE_GROUP_NAME=$RESOURCE_GROUP_NAME" >> $GITHUB_ENV
echo "APP_SERVICE_PLAN_NAME=$APP_SERVICE_PLAN_NAME" >> $GITHUB_ENV
echo "STORAGE_NAME=$STORAGE_NAME" >> $GITHUB_ENV
echo "FILE_SHARE_NAME=$FILE_SHARE_NAME" >> $GITHUB_ENV
echo "APP_INSIGHTS_NAME=$APP_INSIGHTS_NAME" >> $GITHUB_ENV
echo "DASHBOARD_NAME=$DASHBOARD_NAME" >> $GITHUB_ENV
echo "DASHBOARD_DISPLAY_NAME=$DASHBOARD_DISPLAY_NAME" >> $GITHUB_ENV
echo "USER_MANAGED_IDENTITY_NAME=$USER_MANAGED_IDENTITY_NAME" >> $GITHUB_ENV
echo "SUBNET_RESOURCE_ID=$SUBNET_RESOURCE_ID" >> $GITHUB_ENV
echo "DNS_SERVER=$DNS_SERVER" >> $GITHUB_ENV
echo "DNS_ALT_SERVER=$DNS_ALT_SERVER" >> $GITHUB_ENV
echo "SERVICEBUS_CONNECTION_NAME_SP=$SERVICEBUS_CONNECTION_NAME_SP" >> $GITHUB_ENV
echo "SERVICEBUS_NAMESPACE_SP=$SERVICEBUS_NAMESPACE_SP" >> $GITHUB_ENV
echo "ACTION_GROUP_NAME=$ACTION_GROUP_NAME" >> $GITHUB_ENV
echo "SCHEDULED_QUERY_RULE_NAME=$SCHEDULED_QUERY_RULE_NAME" >> $GITHUB_ENV
echo "ALERTING_EMAIL_ADDRESS=$ALERTING_EMAIL_ADDRESS" >> $GITHUB_ENV