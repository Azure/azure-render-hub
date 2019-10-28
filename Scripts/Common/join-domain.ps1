Param(
  [string]$tenantId,
  [string]$applicationId,
  [string]$keyVaultCertificateThumbprint,
  [string]$keyVaultName,
  [string]$domainName = $null,
  [string]$domainOuPath = $null,
  [string]$domainJoinUserName = $null,
)

Import-Module AzureRm
Login-AzureRmAccount -ServicePrincipal -TenantId $tenantId -CertificateThumbprint $keyVaultCertificateThumbprint -ApplicationId $applicationId

if ('' -eq $domainName)
{
    Write-Output "No domain name specified."
    exit 1
}

if (-Not (Get-WmiObject -Class Win32_ComputerSystem).PartOfDomain)
{
    if (-Not ($secrets | Where-Object {$_.Name -eq "DomainJoinUserName"}) -and '' -eq $domainJoinUserName)
    {
        Write-Output "Domain specified, but no username specified or found in key vault."
        exit 1
    }

    if (-Not ($secrets | Where-Object {$_.Name -eq "DomainJoinUserPassword"}))
    {
        Write-Output "Domain specified, but no password found in key vault."
        exit 1
    }

    if ('' -ne $domainJoinUserName)
    {
        $domainUser = $domainJoinUserName
    }
    else
    {
        $domainUser = (Get-AzureKeyVaultSecret -VaultName "$keyVaultName" -Name 'DomainJoinUserName').SecretValueText
    }

    $domainPasswordSecret = Get-AzureKeyVaultSecret -VaultName "$keyVaultName" -Name 'DomainJoinUserPassword'

    Write-Output "Joining domain $domainName with user $domainUser"

    $domainCred = New-Object System.Management.Automation.PSCredential($domainUser, $domainPasswordSecret.SecretValue)

    if ($domainOuPath -and $domainOuPath -ne "")
    {
        Add-Computer -DomainName "$domainName" -OUPath "$domainOuPath" -Credential $domainCred -Restart -Force
    }
    else
    {
        Add-Computer -DomainName "$domainName" -Credential $domainCred -Restart -Force
    }

    # Pause while we wait for the restart to ensure tasks don't run
    Start-Sleep -Seconds 60

    exit 0
}
else
{
    Write-Output "Compute node is already domain joined, skipping."
}
