#!/bin/bash

# Disable history expansion to support passwords with '!'
set +H
set -x
#set -e

env

INSTALLER_PATH="$AZ_BATCH_APP_PACKAGE_deadlineclient"
TENANT_ID=
APP_ID=
KV_NAME=
KV_CERT_THUMB=
DEADLINE_USER="deadline"
DEADLINE_UID=5005
DEADLINE_GROUP="deadline"
DEADLINE_GID=5005
DEADLINE_REPO_PATH=
DEADLINE_LIC_SERVER=
DEADLINE_LIC_MODE=
DEADLINE_REGION=
SMB_SHARES=
NFS_SHARES=
DOMAIN_NAME=
DEADLINE_GROUPS=
DEADLINE_POOLS=

OPTS=`getopt -n 'parse-options' -o i:t:a:n:p:u:d:o:z:r:s:m:g:b:f:d:y:l: --long installerPath:,tenantId:,applicationId:,keyVaultName:,keyVaultCertificateThumbprint:,deadlineRepositoryPath:,deadlineLicenseServer:,deadlineLicenseMode:,deadlineRegion:,smbShares:,nfsShares:,domainName:,deadlineGroups:,deadlinePools: -- "$@"`
if [ $? != 0 ] ; then echo "Failed parsing options." >&2 ; exit 1 ; fi

echo "$OPTS"
eval set -- "$OPTS"

while true; do
  case "$1" in
    -i | --installerPath ) INSTALLER_PATH="$2"; shift 2 ;;
    -t | --tenantId )    TENANT_ID="$2"; shift 2 ;;
    -a | --applicationId ) APP_ID="$2"; shift 2 ;;
    -n | --keyVaultName ) KV_NAME="$2"; shift 2 ;;
    -p | --keyVaultCertificateThumbprint ) KV_CERT_THUMB="$2"; shift 2 ;;
    -r | --deadlineRepositoryPath ) DEADLINE_REPO_PATH="$2"; shift 2 ;;
    -s | --deadlineLicenseServer ) DEADLINE_LIC_SERVER="$2"; shift 2 ;;
    -m | --deadlineLicenseMode ) DEADLINE_LIC_MODE="$2"; shift 2 ;;
    -g | --deadlineRegion ) DEADLINE_REGION="$2"; shift 2 ;;
    -b | --smbShares ) SMB_SHARES="$2"; shift 2 ;;
    -f | --nfsShares ) NFS_SHARES="$2"; shift 2 ;;
    -d | --domainName ) DOMAIN_NAME="$2"; shift 2 ;;
    -y | --deadlineGroups ) DEADLINE_GROUPS="$2"; shift 2 ;;
    -l | --deadlinePools ) DEADLINE_POOLS="$2"; shift 2 ;;
    -- ) shift; break ;;
    * ) break ;;
  esac
done

INSTALLER_PATH="$AZ_BATCH_APP_PACKAGE_deadlineclient"

echo "$(hostname)" > hostname.txt

sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
sudo sh -c 'echo -e "[azure-cli]\nname=Azure CLI\nbaseurl=https://packages.microsoft.com/yumrepos/azure-cli\nenabled=1\ngpgcheck=1\ngpgkey=https://packages.microsoft.com/keys/microsoft.asc" > /etc/yum.repos.d/azure-cli.repo'

#sudo yum update -y
sudo yum install -y azure-cli samba-client samba-common-tools nfs-utils nfs-utils-lib openssl

if [ ! -r "../certs/sha1-${KV_CERT_THUMB}.pfx.pw" ] || [ ! -r "../certs/sha1-${KV_CERT_THUMB}.pfx" ]; then
    echo "Cannot find certificate with thumbprint ${KV_CERT_THUMB}"
    exit 1
fi

PFX_CERT="../certs/sha1-${KV_CERT_THUMB}.pfx"
PFX_CERT_PWORD="${PFX_CERT}.pw"
PEM_CERT="../certs/sha1-${KV_CERT_THUMB}.pem"

openssl pkcs12 -in "$PFX_CERT" -clcerts -out "$PEM_CERT" -nodes -password file:"$PFX_CERT_PWORD"
az login --service-principal --tenant "$TENANT_ID" --username "$APP_ID" -p "$PEM_CERT"

# Fetch the names of available secrets in Key Vault
availableSecrets="`az keyvault secret list --vault-name $KV_NAME --query [].id | grep $KV_NAME | tr -d '\"'`"

# Set the Deadline service user and group, if available
if echo "$availableSecrets" | grep -q "DeadlineServiceUserNameLinux"; then
    DEADLINE_USER="`az keyvault secret show --vault-name $KV_NAME --name DeadlineServiceUserNameLinux --query value | tr -d '\"'`"
fi

if echo "$availableSecrets" | grep -q "DeadlineServiceUserIdLinux"; then
    DEADLINE_UID="`az keyvault secret show --vault-name $KV_NAME --name DeadlineServiceUserIdLinux --query value | tr -d '\"'`"
fi

if echo "$availableSecrets" | grep -q "DeadlineServiceGroupLinux"; then
    DEADLINE_GROUP="`az keyvault secret show --vault-name $KV_NAME --name DeadlineServiceGroupLinux --query value | tr -d '\"'`"
fi

if echo "$availableSecrets" | grep -q "DeadlineServiceGroupIdLinux"; then
    DEADLINE_GID="`az keyvault secret show --vault-name $KV_NAME --name DeadlineServiceGroupIdLinux --query value | tr -d '\"'`"
fi


grep -q "$DEADLINE_GROUP" /etc/group
if [ $? -eq 0 ]; then
    cat /etc/group | grep "$DEADLINE_GROUP" | grep -q "$DEADLINE_GID"
    if [ $? -ne 0 ]; then
        echo "Deadline group $DEADLINE_GROUP exists but has the wrong GID - exiting."
        exit 1
    fi
else
    groupadd -g $DEADLINE_GID $DEADLINE_GROUP
fi


grep -q "$DEADLINE_USER" /etc/passwd
if [ $? -eq 0 ]; then
    cat /etc/passwd | grep "$DEADLINE_USER" | grep -q "$DEADLINE_UID"
    if [ $? -ne 0 ]; then
        echo "Deadline user $DEADLINE_USER exists but has the wrong UID - exiting."
        exit 1
    fi
else
    adduser -u $DEADLINE_UID -g $DEADLINE_GROUP $DEADLINE_USER
fi


# Mount any SMB shares
if [ -n "$SMB_SHARES" ]; then

    SMB_USER="$DEADLINE_USER"
    SMB_PASSWORD="$DEADLINE_PASSWORD"
    
    if echo "$availableSecrets" | grep -q "DomainJoinUserName"; then
        SMB_USER="$(az keyvault secret show --vault-name "$KV_NAME" --name DomainJoinUserName --query value | tr -d '\"')"
        SMB_PASSWORD="$(az keyvault secret show --vault-name "$KV_NAME" --name DomainJoinUserPassword --query value | tr -d '\"')"
        
        if echo "$SMB_USER" | grep -q '\\'; then
            SMB_USER="$(echo $SMB_USER | awk -F'\' '{print $NF}')"
        fi
    fi

    for shareAndMount in $(echo $SMB_SHARES | tr ";" "\n")
    do       
        share="`echo "$shareAndMount" | awk -F'=' '{print $1}'`"
        mount="`echo "$shareAndMount" | awk -F'=' '{print $2}'`"
        
        echo "$mount" | grep -q '^/'
        if [ $? -ne 0 ]; then
            echo "Skipping share $shareAndMount as its either malformed or not a Linux share."
            continue
        fi
        
        mount | grep -q "$mount"
        if [ $? -eq 0 ]; then
            mount | grep -q "$mount" | grep -q "$share"
            if [ $? -eq 0 ]; then
                echo "Share $share is already mounted."
            else
                echo "Another share is already mounted at $mount, exiting."
                exit 1
            fi
            continue
        fi
        
        echo "Mounting SMB share $share at $mount"
        mkdir -p "$mount"
        chown -R $DEADLINE_USER:$DEADLINE_GROUP "$mount"
        options="username=$SMB_USER,password=$SMB_PASSWORD,vers=2.0,uid=$DEADLINE_UID,gid=$DEADLINE_GID"
        if [ -n "$DOMAIN_NAME" ]; then
            options="$options,domain=$DOMAIN_NAME"
        fi
        mount -t cifs "$share" "$mount" -o "$options"
    done
fi


# Mount any NFS shares
if [ -n "$NFS_SHARES" ]; then
    for shareAndMount in $(echo $NFS_SHARES | tr ";" "\n")
    do
        echo "$shareAndMount" | grep -q '^\\'
        if [ $? -eq 0 ]; then
            echo "Skipping share $shareAndMount as its either malformed or not a Linux share."
            continue
        fi
        
        share="`echo "$shareAndMount" | awk -F'=' '{print $1}'`"
        mount="`echo "$shareAndMount" | awk -F'=' '{print $2}'`"
        
        mount | grep -q "$mount"
        if [ $? -eq 0 ]; then
            echo "Share $share is already mounted."
            continue
        fi
        
        echo "Mounting NFS share $share at $mount"
        
        mkdir -p "$mount"
        chown -R $DEADLINE_USER:$DEADLINE_GROUP "$mount"
        mount -t nfs "$share" "$mount"
    done
fi

ARGS=(--mode unattended --debuglevel 4 --repositorydir "$DEADLINE_REPO_PATH" --licensemode "$DEADLINE_LIC_MODE" --region "$DEADLINE_REGION" --slavestartup true --daemonuser "$DEADLINE_USER" --launcherdaemon true)

if echo "$availableSecrets" | grep -q "DeadlineDbClientCertificate"; then
    certPath="/home/$DEADLINE_USER/.certs"
    cert="$certPath/DeadlineClient.pfx"
    
    if [ ! -e "$cert" ]; then
        mkdir -p $certPath
        touch "$cert"
        chown -R $DEADLINE_USER:$DEADLINE_GROUP $certPath
        chmod -r 660 $certPath
        az keyvault secret show --vault-name $KV_NAME --name DeadlineDbClientCertificate --query value | tr -d '\"' | base64 --decode >> "$cert"
    fi
    
    ARGS+=(--dbsslcertificate)
    ARGS+=("$cert")
    
    if echo "$availableSecrets" | grep -q "DeadlineDbClientCertificatePassword"; then
        dbPassword="$(az keyvault secret show --vault-name "$KV_NAME" --name DeadlineDbClientCertificatePassword --query value | tr -d '\"')"
        ARGS+=(--dbsslpassword)
        ARGS+=("$dbPassword")
    fi
fi

if [ -n "$DEADLINE_LIC_SERVER" ]; then
    ARGS+=(--licenseserver)
    ARGS+=("$DEADLINE_LIC_SERVER")
fi

if [ -n "$APP_INSIGHTS_APP_ID" ] && [ -n "$APP_INSIGHTS_INSTRUMENTATION_KEY" ]; then
    # Install and setup Application Insights
    wget  -O - https://raw.githubusercontent.com/Azure/batch-insights/master/centos.sh | bash
fi

DEADLINE_INSTALLER="$(ls -1 ${INSTALLER_PATH}/DeadlineClient-*-linux-x64-installer.run | tail -1)"
if [ -z "$DEADLINE_INSTALLER" ]; then
    echo "Could not find Deadline installer in $INSTALLER_PATH.  Available files:"
    ls -1 ${INSTALLER_PATH}/DeadlineClient-*-linux-x64-installer.run
    exit 1
fi

$INSTALLER_PATH/$(basename $DEADLINE_INSTALLER) "${ARGS[@]}"
result=$?
if [ $result -ne 0 ]; then
    exit $result
fi

if [ -n "$DEADLINE_GROUPS" ]; then
    for group in $(echo $DEADLINE_GROUPS | tr ";" "\n")
    do
        echo "Adding slave to group $group"
        /opt/Thinkbox/Deadline10/bin/deadlinecommand SetGroupsForSlave `hostname` $group
    done
fi

if [ -n "$DEADLINE_POOLS" ]; then
    for pool in $(echo $DEADLINE_POOLS | tr ";" "\n")
    do
        echo "Adding slave to pool $pool"
        /opt/Thinkbox/Deadline10/bin/deadlinecommand SetPoolsForSlave `hostname` $pool
    done
fi