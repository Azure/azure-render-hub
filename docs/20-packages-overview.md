# Packages

Packages are a collection of files that will be deployed to your virtual machines.  A package might be a rendering application like RedShift, a GPU driver or a third party application that you use for post-processing.

There are several built-in package types:

. Deadline 10
. Qube 6.10
. Qube 7.0
. GPU
. General

All except General have preset installation scripts based on the type, and require certain files to be in the package (for example, a Deadline 10 package must include the Deadline 10 client installer).  The Render Farm Manager knows how to install this package type and includes the necessary installation script automatically.

GPU and General packages can specify an optional installation command line.  The package command line will be executed from the root of the package.

## Required Files

**Deadline 10**

Windows: DeadlineClient-10.x.xx.x-windows-installer.exe
Linux: DeadlineClient-10.x.xx.x-linux-x64-installer.run

**Qube 6.10 and 7.10**

Please note the versions below may not match your exact versions, ensure the major versions are correct though.

*Windows*
Python: python-2.7.xx.amd64.msi
Qube Core: qube-core-7.0-x-WIN32-6.3-x64.msi
Qube Worker: qube-worker-7.0-x-WIN32-6.3-x64.msi
Qube Job Types: qube-3dsmaxjt-64-7.0-x.msi

*Linux*
Not Supported Yet

**GPU**

The NVIDIA GPU driver is downloaded as an executable (exe).  Double click the .exe to extract the contents, or use 7zip and then ZIP the contents into a new package.  This step is required because the .exe cannot be silently installed.

Windows: 398.75-tesla-desktop-winserver2016-international.zip
