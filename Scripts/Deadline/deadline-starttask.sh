#!/bin/bash

# Disable history expansion to support passwords with '!'
set +H

INSTALLER_PATH="$AZ_BATCH_APP_PACKAGE_deadlineclient"
TENANT_ID=
APP_ID=
KV_NAME=
KV_CERT_THUMB=
DEADLINE_REPO_PATH=
DEADLINE_REPO_USER=
DEADLINE_LIC_SERVER=
DEADLINE_LIC_MODE=
DEADLINE_REGION=
SMB_SHARES=
NFS_SHARES=
DOMAIN_NAME=
DEADLINE_GROUPS=
DEADLINE_POOLS=
DEADLINE_EXCLUDE_LIMIT_GROUPS=

OPTS=`getopt -n 'parse-options' -o i:t:a:n:p:r:u:s:m:g:d:y:l:e: --long installerPath:,tenantId:,applicationId:,keyVaultName:,keyVaultCertificateThumbprint:,deadlineRepositoryPath:,deadlineRepositoryUserName:,deadlineLicenseServer:,deadlineLicenseMode:,deadlineRegion:,domainName:,deadlineGroups:,deadlinePools:,excludeFromLimitGroups: -- "$@"`
if [ $? != 0 ] ; then echo "Failed parsing options." >&2 ; exit 1 ; fi

echo "Arguments: $@"
eval set -- "$OPTS"

while true; do
  case "$1" in
    -i | --installerPath ) INSTALLER_PATH="$2"; shift 2 ;;
    -t | --tenantId )    TENANT_ID="$2"; shift 2 ;;
    -a | --applicationId ) APP_ID="$2"; shift 2 ;;
    -n | --keyVaultName ) KV_NAME="$2"; shift 2 ;;
    -p | --keyVaultCertificateThumbprint ) KV_CERT_THUMB="$2"; shift 2 ;;
    -r | --deadlineRepositoryPath ) DEADLINE_REPO_PATH="$2"; shift 2 ;;
    -u | --deadlineRepositoryUserName ) DEADLINE_REPO_USER="$2"; shift 2 ;;
    -s | --deadlineLicenseServer ) DEADLINE_LIC_SERVER="$2"; shift 2 ;;
    -m | --deadlineLicenseMode ) DEADLINE_LIC_MODE="$2"; shift 2 ;;
    -g | --deadlineRegion ) DEADLINE_REGION="$2"; shift 2 ;;
    -d | --domainName ) DOMAIN_NAME="$2"; shift 2 ;;
    -y | --deadlineGroups ) DEADLINE_GROUPS="$2"; shift 2 ;;
    -l | --deadlinePools ) DEADLINE_POOLS="$2"; shift 2 ;;
    -e | --excludeFromLimitGroups ) DEADLINE_EXCLUDE_LIMIT_GROUPS="$2"; shift 2 ;;
    -- ) shift; break ;;
    * ) break ;;
  esac
done

# Stop the launcher and slave from starting (yet)
systemctl stop deadline10launcher.service

# When Batch recovers a VM, or a VM is pre-empted, a new
# VM replaces the old one.  While the compute node Id doesn't
# change, the hostname does and we want to prevent a new host from
# registering itself in Deadline.  To do this we'll create a hash
# of the pool and compute node Id which will be predictable.
hash=$(echo "$AZ_BATCH_POOL_ID-$AZ_BATCH_NODE_ID" | md5sum)
newHostname=${hash/  -}
echo "Updating hostname from `hostname` to $newHostname"
hostnamectl set-hostname $newHostname

# Set any app licenses system wide.
if [ -n "$AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN" ]; then
    accountUrl=$AZ_BATCH_ACCOUNT_URL
    if [[ "$accountUrl" == */ ]]; then
        accountUrl=${accountUrl::-1}
    fi
    echo export AZ_BATCH_ACCOUNT_URL=\"$accountUrl\" > /etc/profile.d/ses.sh
    echo export AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN=\"$AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN\" >> /etc/profile.d/ses.sh
fi


if [ -n "$DEADLINE_REGION" ]; then
    /opt/Thinkbox/Deadline10/mono/bin/mono "/opt/Thinkbox/Deadline10/bin/deadlinecommand.exe" SetIniFileSetting Region
fi


if [ -n "$DEADLINE_GROUPS" ]; then
    groups=$(echo $DEADLINE_GROUPS | tr ';' ',')
    echo "Adding slave to group(s) $groups"
    /opt/Thinkbox/Deadline10/bin/deadlinecommand SetGroupsForSlave `hostname` $groups
fi


if [ -n "$DEADLINE_POOLS" ]; then
    pools=$(echo $DEADLINE_POOLS | tr ';' ',')
    echo "Adding slave to pool(s) $pools"
    /opt/Thinkbox/Deadline10/bin/deadlinecommand SetPoolsForSlave `hostname` $pools
fi


if [ -n "$DEADLINE_EXCLUDE_LIMIT_GROUPS" ]; then
    wget -O limitgroups.py 'https://raw.githubusercontent.com/Azure/azure-render-farm-manager/master/Scripts/Deadline/limitgroups.py'
    limitgroups=$(echo $DEADLINE_EXCLUDE_LIMIT_GROUPS | tr ';' '\n')
    for group in $limitgroups; do
        echo "Adding slave to limit group $group"
        /opt/Thinkbox/Deadline10/bin/deadlinecommand -ExecuteScriptNoGui limitgroups.py --limitgroups $group --slave $HOSTNAME --exclude
    done
fi

# Set the Pool and Compute Node name in the slave's description and extra data
/opt/Thinkbox/Deadline10/bin/deadlinecommand SetSlaveSetting `hostname` SetSlaveSetting "Pool: $AZ_BATCH_POOL_ID, ComputeNode: $AZ_BATCH_NODE_ID"
/opt/Thinkbox/Deadline10/bin/deadlinecommand SetSlaveExtraInfoKeyValue `hostname` PoolName $AZ_BATCH_POOL_ID
/opt/Thinkbox/Deadline10/bin/deadlinecommand SetSlaveExtraInfoKeyValue `hostname` ComputeNodeName $AZ_BATCH_NODE_ID

# Restart the Deadline Launcher
systemctl start deadline10launcher.service

if [ -n "$APP_INSIGHTS_INSTRUMENTATION_KEY" ]; then
    # Install and setup Application Insights for CPU/GPU and process
    # metrics
    /bin/bash -c 'wget  -O - https://raw.githubusercontent.com/Azure/batch-insights/v1.3.0/scripts/run-linux.sh | bash'
fi
