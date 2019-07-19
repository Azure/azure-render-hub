#!/bin/bash

# Allowed NFS clients IP or address range (CIDR block)
# e.g. 10.2.0.0/24
ALLOWED_HOSTS="$1"

# The export path
EXPORT="$2"

yum install -y nfs-utils rpcbind rsync mdadm 

if [ -z "$ALLOWED_HOSTS" ]; then
    ALLOWED_HOSTS="*"
fi

if [ -z "$EXPORT" ]; then
    echo "No export specified"
    exit 1
fi

mountPoint="$(dirname $EXPORT)"
mkdir -p $mountPoint
createdPartitions=""

# Loop through and partition disks until not found
for disk in sdc sdd sde sdf sdg sdh sdi sdj sdk sdl sdm sdn sdo sdp sdq sdr; do
    fdisk -l /dev/$disk || break
    fdisk /dev/$disk << EOF
n
p
1


t
fd
w
EOF
    createdPartitions="$createdPartitions /dev/${disk}1"
done

sleep 15

# Create RAID-0 volume
if [ -n "$createdPartitions" ]; then
    devices=`echo $createdPartitions | wc -w`
    mdadm --create /dev/md10 --level 0 --raid-devices $devices $createdPartitions
    mkfs -t ext4 /dev/md10
    echo "/dev/md10 $mountPoint ext4 defaults,nofail 0 2" >> /etc/fstab
    sleep 15
    mount /dev/md10
fi

rm /etc/exports
mkdir -p $EXPORT
chown nfsnobody:nfsnobody $EXPORT
chmod -R 770 $EXPORT
echo "$EXPORT    $ALLOWED_HOSTS(rw,sync,no_root_squash,all_squash,anonuid=65534,anongid=65534)" >> /etc/exports

systemctl enable nfs-server.service
systemctl start nfs-server.service

echo "Done"
