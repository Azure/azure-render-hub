Param(
  [string]$installerPath,
  [switch]$domainJoin,
  [string]$domainName = $null,
  [string]$domainOuPath = $null,
  [string]$domainJoinUserName = $null,
  [string]$tenantId,
  [string]$applicationId,
  [string]$keyVaultCertificateThumbprint,
  [string]$keyVaultName,
  [string]$groups = $null
)

$ErrorActionPreference = "Stop"

hostname | Out-File hostname.txt

# Set any app licenses system wide.
if ($env:AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN)
{
    $accountUrl = $env:AZ_BATCH_ACCOUNT_URL
    if ($accountUrl.EndsWith('/'))
    {
        $accountUrl = $accountUrl.TrimEnd('/')
    }
    [Environment]::SetEnvironmentVariable("AZ_BATCH_ACCOUNT_URL", "$accountUrl","Machine")
    [Environment]::SetEnvironmentVariable("AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN", "$env:AZ_BATCH_SOFTWARE_ENTITLEMENT_TOKEN","Machine")
}

# Increase the FLEXLM timeout for high latency VNet links
[Environment]::SetEnvironmentVariable("FLEXLM_TIMEOUT", "5000000", "Machine")
$env:FLEXLM_TIMEOUT = "5000000"

Get-PackageProvider -Name NuGet -Force
Install-Module PowerShellGet -Force

if (Get-Module -ListAvailable -Name AzureRm) {
    Write-Output "AzureRm already installed"
} else {
    Write-Output "Installing AzureRm"
    Install-Module -Name AzureRm -Repository PSGallery -Force
}

Import-Module AzureRm
Login-AzureRmAccount -ServicePrincipal -TenantId $tenantId -CertificateThumbprint $keyVaultCertificateThumbprint -ApplicationId $applicationId

$secrets = Get-AzureKeyVaultSecret -VaultName "$keyVaultName"
$secrets | % { Write-Output "Found secret " $_.Name }

if ($domainJoin)
{
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
}

if ('' -ne $installerPath)
{
    $tractorInstaller = Get-ChildItem $installerPath | where {$_.Name -like "Tractor-2.*_*-windows*.x86_64.msi"}

    if (!$tractorInstaller)
    {
        Write-Host "Could not find the Tractor 2.x installer in $installerPath"
        exit 1
    }
    
    $fullPath = $tractorInstaller.FullName
    
    Write-Output "Installing Tractor using installer $fullPath"
    
    Start-Process msiexec.exe -ArgumentList "/passive /i $fullPath" -Wait
    
    $service = Get-Service | Where { $_.Name -Like "Pixar Tractor Blade Service 2.*" } | Select-Object -Last 1
    
    Write-Output "Setting Tractor service to Automatic startup"
    
    Set-Service $service.Name -StartupType Automatic
}

$service = Get-Service | Where { $_.Name -Like "Pixar Tractor Blade Service 2.*" } | Select-Object -Last 1
if ($service)
{
    if ($service.Status -eq 'Running')
    {
        # Stop the service so we can update blade config
        Write-Output "Stopping $($service.Name)"
        Stop-Service $service.Name
        Start-Sleep -Seconds 2
    }
}

if ('' -ne $groups)
{
    # Tractor Groups
    $groups = $groups.Replace(';', ',')
    Write-Output "Adding blade to groups $groups"
}

if ($service)
{
    # Restart the service
    Write-Output "Starting $($service.Name)"
    Start-Service $service.Name
}

# App insights support
if ($env:APP_INSIGHTS_APP_ID -and $env:APP_INSIGHTS_INSTRUMENTATION_KEY)
{
    # Install Batch Insights
    iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/Azure/batch-insights/v1.2.0/scripts/run-windows.ps1')) | Out-File batchinsights.log
}

if (!$service)
{
    # The tractor blade isn't running as a service so we need to block
    # to keep the process alive.
    while($true)
    {
        Start-Sleep -Seconds 86400
    }
}
