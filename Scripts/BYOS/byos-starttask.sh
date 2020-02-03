#!/bin/bash

# Disable history expansion to support passwords with '!'
set +H

INSTALLER_PATH=
TENANT_ID=
APP_ID=
KV_NAME=
KV_CERT_THUMB=
SCHEDULER_HOST=
BLADE_GROUPS=

OPTS=`getopt -n 'parse-options' -o t:a:k:c:h:g: --long tenantId:,applicationId:,keyVaultName:,keyVaultCertificateThumbprint:,host:,groups: -- "$@"`
if [ $? != 0 ] ; then echo "Failed parsing options." >&2 ; exit 1 ; fi

echo "Arguments: $@"
eval set -- "$OPTS"

while true; do
  case "$1" in
    -t | --tenantId )    TENANT_ID="$2"; shift 2 ;;
    -a | --applicationId ) APP_ID="$2"; shift 2 ;;
    -k | --keyVaultName ) KV_NAME="$2"; shift 2 ;;
    -c | --keyVaultCertificateThumbprint ) KV_CERT_THUMB="$2"; shift 2 ;;
    -h | --host ) SCHEDULER_HOST="$2"; shift 2 ;;
    -g | --groups ) BLADE_GROUPS="$2"; shift 2 ;;
    -- ) shift; break ;;
    * ) break ;;
  esac
done


# When Batch recovers a VM, or a VM is pre-empted, a new
# VM replaces the old one.  While the compute node Id doesn't
# change, the hostname does and we want to prevent a new host from
# registering itself in OpenCue.  To do this we'll create a hash
# of the pool and compute node Id which will be predictable.
HOSTNAME_PREFIX="az-"
hash=$(echo "$AZ_BATCH_POOL_ID-$AZ_BATCH_NODE_ID" | md5sum | cut -c 1-8)
newHostname="${HOSTNAME_PREFIX}${hash}"
echo "Updating hostname `hostname` to $newHostname"
hostnamectl set-hostname $newHostname


# Application Licenses
if [ -n "$AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN" ]; then

    # Pay-per-use licensing requires a URL and Token to
    # be available to the renderer.  Here we set it globally.  
    # Any render process needs to have the below envionrment.
    accountUrl=$AZ_BATCH_ACCOUNT_URL
    if [[ "$accountUrl" == */ ]]; then
        accountUrl=${accountUrl::-1}
    fi
    echo export AZ_BATCH_ACCOUNT_URL=\"$accountUrl\" > /etc/profile.d/ses.sh
    echo export AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN=\"$AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN\" >> /etc/profile.d/ses.sh
fi


if [ -n "$SCHEDULER_HOST" ]; then
    echo "Setting scheduler host to $SCHEDULER_HOST"
    # Add any logic to update or set the scheduler host or IP here.
fi


if [ -n "$BLADE_GROUPS" ]; then
    groups=$(echo $BLADE_GROUPS | tr ';' ',')
    # Set any blade groups here
fi


# Install Telemetry Agent
if [ -n "$APP_INSIGHTS_INSTRUMENTATION_KEY" ]; then
    # Install and setup Application Insights for CPU/GPU and process metrics.
    # These are required for auto-scale.
    wget -O run-linux.sh https://raw.githubusercontent.com/Azure/batch-insights/v1.3.0/scripts/run-linux.sh
    chmod +x run-linux.sh
    ./run-linux.sh 2>&1
fi

exit 0
