# Packages

Packages are a collection of files that will be deployed to your virtual machines.  A package might be a renderign application like RedShift, a GPU driver or a third party application that you use for post processing.

There are several package types:

. Deadline 10
. Qube 6.10
. Qube 7.0
. GPU
. General

Where all but general have preset installation scripts based on the type.  For example, a Deadline 10 package must include the Deadline 10 client installer.  The Render Farm Manager knows how to install this package type.

GPU and general packages can specify an optional installation command line.  The package command line will be executed from the root of the package.
