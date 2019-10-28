#!/bin/bash

# Disable history expansion to support passwords with '!'
set +H

INSTALLER_PATH=
TENANT_ID=
APP_ID=
KV_NAME=
KV_CERT_THUMB=
TRACTOR_ENGINE=
TRACTOR_GROUPS=

OPTS=`getopt -n 'parse-options' -o i:t:a:k:c:e:g: --long installerPath:,tenantId:,applicationId:,keyVaultName:,keyVaultCertificateThumbprint:,engineHost:,groups: -- "$@"`
if [ $? != 0 ] ; then echo "Failed parsing options." >&2 ; exit 1 ; fi

echo "Arguments: $@"
eval set -- "$OPTS"

while true; do
  case "$1" in
    -i | --installerPath ) INSTALLER_PATH="$2"; shift 2 ;;
    -t | --tenantId )    TENANT_ID="$2"; shift 2 ;;
    -a | --applicationId ) APP_ID="$2"; shift 2 ;;
    -k | --keyVaultName ) KV_NAME="$2"; shift 2 ;;
    -c | --keyVaultCertificateThumbprint ) KV_CERT_THUMB="$2"; shift 2 ;;
    -e | --engineHost ) TRACTOR_ENGINE="$2"; shift 2 ;;
    -g | --groups ) TRACTOR_GROUPS="$2"; shift 2 ;;
    -- ) shift; break ;;
    * ) break ;;
  esac
done

# Stop the blade service
systemctl | grep tractor-blade > /dev/null
if [ $? -eq 0 ]; then
    systemctl stop tractor-blade.service
fi

# When Batch recovers a VM, or a VM is pre-empted, a new
# VM replaces the old one.  While the compute node Id doesn't
# change, the hostname does and we want to prevent a new host from
# registering itself in Tractor.  To do this we'll create a hash
# of the pool and compute node Id which will be predictable.
HOSTNAME_PREFIX="azure-blade-"
hash=$(echo "$AZ_BATCH_POOL_ID-$AZ_BATCH_NODE_ID" | md5sum | cut -c 1-8)
newHostname="${HOSTNAME_PREFIX}${hash}"
echo "Updating hostname `hostname` to $newHostname"
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


if [ -n "$INSTALLER_PATH" ]; then
    
    # Find the installer RPM
    installer=$(find ./$INSTALLER_PATH -name 'Tractor-2.*.rpm' | head -1)
    
    if [ -e "$installer" ]; then
        # Install Tractor blade
        
        echo "Installing $installer"
        
        rpm -i $installer
        
        # Get Tractor version, 2.2 or 2.3
        version=$(echo $installer | grep -oP 'Tractor-\K(2.[23])')
        
        if [ -n "$TRACTOR_ENGINE" ]; then
            echo "Setting Tractor Engine override to $TRACTOR_ENGINE"
        
            # Tractor 2.3 has a bug in the systemd script that points to Tractor 2.2
            echo "BIN=/opt/pixar/Tractor-${version}/bin" > /etc/sysconfig/tractor-blade
            
            # Override the Tractor engine hostname and port, if specified.
            echo "OPTIONS=\"--engine $TRACTOR_ENGINE\"" >> /etc/sysconfig/tractor-blade
        fi
        
        # Copy the systemd service script
        cp /opt/pixar/Tractor-${version}/lib/SystemServices/systemd/tractor-blade.service /usr/lib/systemd/system
        
        # Enable the service
        systemctl enable tractor-blade.service
        
        systemctl daemon-reload
    else
        echo "No Tractor installer RPM found in $INSTALLER_PATH"
    fi
fi


if [ -n "$TRACTOR_GROUPS" ]; then
    groups=$(echo $TRACTOR_GROUPS | tr ';' ',')
    # TODO - We need to add this blade into the specified groups
fi

echo "Starting Tractor Blade service"

# Start the Tractor Blade service
systemctl start tractor-blade.service

if [ -n "$APP_INSIGHTS_INSTRUMENTATION_KEY" ]; then
    # Install and setup Application Insights for CPU/GPU and process metrics
    wget -O run-linux.sh https://raw.githubusercontent.com/Azure/batch-insights/v1.3.0/scripts/run-linux.sh
    chmod +x run-linux.sh
    ./run-linux.sh 2>&1
fi

systemctl status tractor-blade.service
