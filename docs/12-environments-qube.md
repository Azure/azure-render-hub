# Qube! Configuration

The Qube! configuration only requires the Superviser IP address.  This is used if Qube! is being installed on
render nodes via a Qube! [Package](20-packages-overview.md).

## Domain

The domain details are required if you want your Windows virtual machines to join your domain.  You'll need to specify your domain name
and credentials with permissions to join a computer to the domain.

Note that your VNet DNS server must be configured to forward DNS requests to you domain, or set to your domains DNS server.  In short
the virtual machines on the Azure VNet must be able to resolve your domain name.

Return to the Render Hub [docs](README.md).
