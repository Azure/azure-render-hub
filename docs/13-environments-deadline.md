# Deadline Configuration

The Deadline 10 configuration page allows you to specify your Deadline configuration which is used to install or configure your Deadline slaves.

## Domain

The domain details are required if you want your Windows virtual machines to join your domain.  You'll need to specify your domain name
and credentials with permissions to join a computer to the domain.

Note that your VNet DNS server must be configured to forward DNS requests to you domain, or set to your domains DNS server.  In short
the virtual machines on the Azure VNet must be able to resolve your domain name.

## Deadline 10 configuration

In this section you specify you Deadline repository share and any credentials that might be required to access it.  The repository 
configuration is used to join the virtual machines to Deadline Groups and Pools.

The repository share is required.

## Deadline 10 Installation Parameters (Optional)

This section is only required if you'll be installing the Deadline Client agent on virtual machines using a [Package](20-packages-overview.md).

**License Mode**
Either Standard or License Free with the latter supporting up to two Deadline slaves without a license.

**License Server**
The IP and port of the Deadline license server.  Only required if using standard licensing.

The format is <port>@<ip>, with the default port being 27008@<IP>.

**Deadline Region**
Optional Deadline region all virtual machines in this environment will be joined to.  The region must already be created in Deadline.

**Run Client as Server**
When installing the Deadline Launcher and Slave, install as a service using the specified service username and password.

Note that this option isn't recommended on Windows as various Deadline functionality will not work as expected without being launched in
an interactive session.

***Deadline DB Requires Certificate**
Check this box if the Deadline Mongo DB requires a client certificate for authentication.

**Deadline Database Certificate**
The database client certificate/PFX file if required.

**Deadline Database Certificate Password**
The password to the above certificate, if required.
