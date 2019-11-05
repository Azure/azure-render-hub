#!/bin/bash

# Disable history expansion to support passwords with '!'
set +H

INSTALLER_PATH=
TENANT_ID=
APP_ID=
KV_NAME=
KV_CERT_THUMB=
CUEBOT_HOST=
OPENCUE_GROUPS=
OPENCUE_FACILITY=

OPTS=`getopt -n 'parse-options' -o i:t:a:k:c:e:g:f: --long installerPath:,tenantId:,applicationId:,keyVaultName:,keyVaultCertificateThumbprint:,cuebotHost:,groups:,facility: -- "$@"`
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
    -h | --cuebotHost ) CUEBOT_HOST="$2"; shift 2 ;;
    -f | --facility ) OPENCUE_FACILITY="$2"; shift 2 ;;
    -g | --groups ) OPENCUE_GROUPS="$2"; shift 2 ;;
    -- ) shift; break ;;
    * ) break ;;
  esac
done

SERVICE_NAME="opencue-rqd.service"

# Stop the RQD service
systemctl | grep $SERVICE_NAME > /dev/null
if [ $? -eq 0 ]; then
    systemctl stop $SERVICE_NAME
fi

# When Batch recovers a VM, or a VM is pre-empted, a new
# VM replaces the old one.  While the compute node Id doesn't
# change, the hostname does and we want to prevent a new host from
# registering itself in OpenCue.  To do this we'll create a hash
# of the pool and compute node Id which will be predictable.
HOSTNAME_PREFIX="azure-"
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

    # Find the installer
    installer=$(find ./$INSTALLER_PATH -name 'rqd-*-all.tar.gz' | head -1)

    if [ -e "$installer" ]; then
        # Install OpenCue RQD

        echo "Installing $installer"

        n=0
        while true
        do
            if [ $n -ge 5 ]; then
                echo "Failed to install yum packages epel-release gcc python-devel time"
                exit 1
            fi
            
            yum install -y install epel-release gcc python-devel time && break  # substitute your command here
            
            n=$[$n+1]
            s=$((1 + RANDOM % 15))
            sleep $s
        done

        n=0
        while true
        do
            if [ $n -ge 5 ]; then
                echo "Failed to install yum packages python-pip"
                exit 1
            fi
            
            yum install -y python-pip && break  # substitute your command here
            
            n=$[$n+1]
            s=$((1 + RANDOM % 15))
            sleep $s
        done

        tar xvfz "$installer"

        rqd_dir=$(basename -s '.tar.gz' $installer)

        cd $rqd_dir

        sudo -H pip install -r requirements.txt

        sudo -H python setup.py install

cat > opencue-rqd.service <<'EOF'
[Unit]
Description=OpenCue RQD Service
Wants=network.target
After=network.target

[Service]
Environment=OPTIONS=
Environment=BIN=/usr/bin
EnvironmentFile=-/etc/sysconfig/opencue-rqd
ExecStart=/bin/bash -c "${BIN}/rqd ${OPTIONS}"
LimitNOFILE=500000
LimitNPROC=500000
StandardOutput=syslog+console
StandardError=syslog+console
SyslogIdentifier=rqd

[Install]
WantedBy=multi-user.target
EOF

        # Copy the systemd service script
        cp opencue-rqd.service /usr/lib/systemd/system

        # Enable the service
        systemctl enable $SERVICE_NAME

        systemctl daemon-reload
    else
        echo "No OpenCue installer tarball found in $INSTALLER_PATH"
    fi
fi

if [ -n "$OPENCUE_FACILITY" ]; then
    if [ -e "/etc/opencue/rqd.conf" ]; then
        grep -q '^DEFAULT_FACILITY=' /etc/opencue/rqd.conf
        if [ $? -eq 0 ]; then
            # Update the existing facility
            sed -i .bak "s/DEFAULT_FACILITY=.*/DEFAULT_FACILITY=$OPENCUE_FACILITY/" /etc/opencue/rqd.conf
        else
            echo "DEFAULT_FACILITY=$OPENCUE_FACILITY" >> /etc/opencue/rqd.conf
        fi
    else
        # Set the default facility
        mkdir /etc/opencue
        echo "[Override]" > /etc/opencue/rqd.conf
        echo "DEFAULT_FACILITY=$OPENCUE_FACILITY" >> /etc/opencue/rqd.conf
    fi
fi


if [ -n "$CUEBOT_HOST" ]; then
    
    echo "Setting Cuebot host to $CUEBOT_HOST"
    
    if [ -e "/etc/opencue/rqd.conf" ]; then
        grep -q '^OVERRIDE_CUEBOT=' /etc/opencue/rqd.conf
        if [ $? -eq 0 ]; then
            # Update the existing facility
            sed -i .bak "s/OVERRIDE_CUEBOT=.*/OVERRIDE_CUEBOT=$CUEBOT_HOST/" /etc/opencue/rqd.conf
        else
            echo "OVERRIDE_CUEBOT=$CUEBOT_HOST" >> /etc/opencue/rqd.conf
        fi
    else
        # Set the default facility
        mkdir /etc/opencue
        echo "[Override]" > /etc/opencue/rqd.conf
        echo "OVERRIDE_CUEBOT=$CUEBOT_HOST" >> /etc/opencue/rqd.conf
    fi
fi


if [ -n "$OPENCUE_GROUPS" ]; then
    groups=$(echo $OPENCUE_GROUPS | tr ';' ',')
    # TODO - We need to add this RQD into the specified groups
fi


systemctl start $SERVICE_NAME


if [ -n "$APP_INSIGHTS_INSTRUMENTATION_KEY" ]; then
    # Install and setup Application Insights for CPU/GPU and process metrics
    wget -O run-linux.sh https://raw.githubusercontent.com/Azure/batch-insights/v1.3.0/scripts/run-linux.sh
    chmod +x run-linux.sh
    ./run-linux.sh 2>&1
fi

systemctl status $SERVICE_NAME

exit 0
