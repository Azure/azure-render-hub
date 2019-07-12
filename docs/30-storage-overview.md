# Storage Options

Each environment creates an Azure Files share, but this isn't always sufficient if you have hundreds or thousands of virtual machines in
your render farm due to the IOPS and throughput required to support these higher numbers of virtual machines.

The Render Farm Manager supports two storage options that target different sized render farms, a File Server (NFS) or Avere vFXT Cluster.  Each are described below.

# File Server (NFS)

The File Server option deploys a dedicated Linux (CentOS 7.6) virtual machine with a single NFS export.  The virtual machine has 8 disks, configured and formatted in a RAID 0 setup to offer maximum IOPS.

This option is recommended for render farms with tens or low hundreds of virtual machines.

Due to the deployment being a single File Server it is not highly-available.

For more in

For details on deploying a File Server see [here](31-storage-fileserver-deploy.md).

# Avere vFXT Cluster

For render farms with hundreds or thousands of virtual machines the Avere vFXT Cache is the recommended option.  The cluster caches input data in memory and is resilient to a node failure.  An Avere vFXT Cluster can scale from 3 to 12 virtual machines, increasing the IOPS and throughput.

for more information on Avere vFXT see [here](https://azure.microsoft.com/en-au/services/storage/avere-vfxt/).

For details on deploying an Avere vFXT Cluster see [here](32-storage-avere-deploy.md).
