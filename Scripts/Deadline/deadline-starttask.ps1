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
  [string]$deadlineRepositoryPath,
  [string]$deadlineRepositoryUserName = $null,
  [string]$deadlineServiceUserName = $null,
  [string]$deadlineLicenseServer = $null,
  [string]$deadlineLicenseMode,
  [string]$deadlineRegion,
  [string]$deadlineGroups = $null,
  [string]$deadlinePools = $null,
  [string]$excludeFromLimitGroups = $null
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

# Use the username is not specified, use the KV, if it exists
if (('' -eq $deadlineServiceUserName) -and ($secrets | Where-Object {$_.Name -eq "DeadlineServiceUserName"}))
{
    $deadlineServiceUserName = (Get-AzureKeyVaultSecret -VaultName "$keyVaultName" -Name 'DeadlineServiceUserName').SecretValueText
}

$deadlineServiceUserPassword = ''
if ($secrets | Where-Object {$_.Name -eq "DeadlineServiceUserPassword"})
{
    $deadlineServiceUserPassword = (Get-AzureKeyVaultSecret -VaultName "$keyVaultName" -Name 'DeadlineServiceUserPassword').SecretValueText
}

if (('' -ne $deadlineServiceUserName) -and ('' -eq $deadlineServiceUserPassword))
{
    Write-Output "A service username was specified, but no service password was found."
    exit 1
}

# Set the Deadline repo username
if (('' -eq $deadlineRepositoryUserName) -and ($secrets | Where-Object {$_.Name -eq "DeadlineShareUserName"}))
{
    $deadlineRepositoryUserName = (Get-AzureKeyVaultSecret -VaultName "$keyVaultName" -Name 'DeadlineShareUserName').SecretValueText
}

# Set the Deadline repo password
$deadlineRepositoryPassword = ''
if ($secrets | Where-Object {$_.Name -eq "DeadlineShareUserPassword"})
{
    $deadlineRepositoryPassword = (Get-AzureKeyVaultSecret -VaultName "$keyVaultName" -Name 'DeadlineShareUserPassword').SecretValueText
}

if (('' -ne $deadlineRepositoryUserName) -and ('' -eq $deadlineRepositoryPassword))
{
    Write-Output "A repository username was specified, but no password was found."
    exit 1
}

# Use the repo user/password for connection to the share,
# otherwise fallback to the share user/password
if ('' -ne $deadlineRepositoryUserName)
{
    $shareUsername = $deadlineRepositoryUserName
    $sharePassword = $deadlineRepositoryPassword
}
elseif ('' -ne $deadlineServiceUserName)
{
    $shareUsername = $deadlineServiceUserName
    $sharePassword = $deadlineServiceUserPassword
}

$deadlinePath = "$env:DEADLINE_PATH"
if ('' -eq $deadlinePath)
{
    # Fall back to default path
    $deadlinePath = "C:\Program Files\Thinkbox\Deadline10\bin"
}

# Check if the Deadline Client is already installed
if ((Test-Path "$deadlinePath\deadlinecommand.exe") -and (Test-Path "C:\ProgramData\Thinkbox"))
{
    # We need to ensure the config is set to "NoGui" otherwise the slave wont start.
    Get-Childitem 'C:\ProgramData\Thinkbox' -Recurse -Include deadline.ini | ForEach {
        (Get-Content $_.FullName) `
            -Replace 'NoGuiMode=.*', 'NoGuiMode=True' `
            -Replace 'LaunchSlaveAtStartup=.*', 'LaunchSlaveAtStartup=True' | Set-Content $_.FullName
    }
    
    if ('' -ne $deadlineRegion)
    {
        Get-Childitem 'C:\ProgramData\Thinkbox' -Recurse -Include deadline.ini | ForEach {
            (Get-Content $_.FullName) `
                -Replace 'Region=.*', "Region=$deadlineRegion" | Set-Content $_.FullName
        }
    }
}
elseif ('' -ne $installerPath -and (Test-Path $installerPath))
{
    # There's no Deadline client installed, lets install it...

    #$escapedRepoPath = $deadlineRepositoryPath -Replace '\\','/'

    $baseArgs = @("--mode", "unattended", "--debuglevel", "4", "--repositorydir", "$deadlineRepositoryPath", "--slavestartup", "true", "--launcherstartup", "true")
    $installerArgs = {$baseArgs}.Invoke()

    # Find the installer in the path
    $installer = Get-ChildItem "$installerPath" | where {$_.Name -like "DeadlineClient-*-windows-installer.exe"}
    if (-Not $installer)
    {
        Write-Output "Cannot find Deadline client installer in app package.  Files found:"
        Get-ChildItem "$installerPath"
        exit 1
    }

    if ('' -ne $deadlineServiceUserName)
    {
        $installerArgs.Add("--serviceuser")
        $installerArgs.Add("`"$deadlineServiceUserName`"")

        $installerArgs.Add("--servicepassword")
        $installerArgs.Add("`"$deadlineServiceUserPassword`"")

        $installerArgs.Add("--launcherservice")
        $installerArgs.Add("true")
    }

    $installerArgs.Add("--noguimode")
    $installerArgs.Add("true")

    $installerArgs.Add("--licensemode")
    $installerArgs.Add($deadlineLicenseMode)

    if ('' -ne $deadlineRegion)
    {
        $installerArgs.Add("--region")
        $installerArgs.Add($deadlineRegion)
    }

    $certDir = "C:\Certs"
    $certPath = "$certDir\DeadlineClient.pfx"
    if ($secrets | Where-Object {$_.Name -eq "DeadlineDbClientCertificate"})
    {
        $installerArgs.Add("--dbsslcertificate")
        $installerArgs.Add("`"$certPath`"")

        $cert = Get-AzureKeyVaultSecret -VaultName "$keyVaultName" -Name 'DeadlineDbClientCertificate'
        $certBytes = [System.Convert]::FromBase64String($cert.SecretValueText)
        $certCollection = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2Collection
        $certCollection.Import($certBytes,$null,[System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)

        $certPassword = $null
        if (($secrets | Where-Object {$_.Name -eq "DeadlineDbClientCertificatePassword"}))
        {
            $certPassword = (Get-AzureKeyVaultSecret -VaultName "$keyVaultName" -Name 'DeadlineDbClientCertificatePassword').SecretValueText
            $installerArgs.Add("--dbsslpassword")
            $installerArgs.Add("`"$certPassword`"")
        }

        mkdir $certDir -Force
        takeown /R /F $certDir
        icacls "$certDir" /grant ${deadlineServiceUserName}:`(OI`)`(CI`)R /T
        icacls "$certDir" /remove Everyone /T
        $protectedCertificateBytes = $certCollection.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pkcs12, $certPassword)
        [System.IO.File]::WriteAllBytes($certPath, $protectedCertificateBytes)

        Write-Output "Installed Deadline DB certificate to $certDir"
    }

    if ('' -ne $deadlineLicenseServer)
    {
        $installerArgs.Add("--licenseserver")
        $installerArgs.Add($deadlineLicenseServer)
    }

    Write-Output "Executing: $($installer.FullName) $installerArgs"
    & cmd.exe /c $($installer.FullName) $installerArgs
    $installerResult = $LastExitCode

    if ($installerResult -ne 0)
    {
        Write-Output "Deadline client installation failed with exit code $installerResult"
        exit $installerResult
    }

    # This wont be in the session env vars so we need to grab it from the registry.
    $deadlinePath = (Get-ItemProperty -Path 'Registry::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Session Manager\Environment' -Name DEADLINE_PATH).DEADLINE_PATH
}
else
{
    Write-Output "The Deadline client is not installed and no installer was found."
    exit 1
}

# Login to the repo share
$repositoryRoot = & "$deadlinePath\deadlinecommand.exe" GetRepositoryRoot
Write-Output "Logging into Deadline repository share $repositoryRoot with user $shareUsername"
net use "$repositoryRoot" /User:$shareUsername "$sharePassword"

# Get the version
$deadlineVersion = & "$deadlinePath\deadlinecommand.exe" Version
Write-Output "Deadline Client $deadlineVersion is installed."

$deadlineRunningAsService = $false

$service = Get-Service deadline10launcherservice -EA Ignore
if ($service)
{
    $deadlineRunningAsService = $true

    if ($service.Status -eq 'Running')
    {
        Stop-Service deadline10launcherservice
        Start-Sleep -Seconds 2
        Stop-Process -Name deadlinelauncherservice -Force
        Start-Sleep -Seconds 2
    }

    Start-Service deadline10launcherservice
}
else
{
    # If we're not running as a domain,
    # ensure deadline launcher is running as the current user
    & "$deadlinePath\\deadlinelauncher.exe"
}

function ExecuteDeadlineCommand
{
    Param(
        [parameter(Mandatory=$true)]
        [String[]]$ArgumentList
    )

    Write-Output "Command: $deadlinePath\deadlinecommand.exe $ArgumentList"
    Start-Process -FilePath "$deadlinePath\deadlinecommand.exe" -ArgumentList $ArgumentList -NoNewWindow -Wait
}

# Set the Deadline slave description to the compute node name
ExecuteDeadlineCommand -ArgumentList SetSlaveSetting,$env:COMPUTERNAME,SlaveDescription,"${env:AZ_BATCH_POOL_ID}-${env:AZ_BATCH_NODE_ID}"
ExecuteDeadlineCommand -ArgumentList SetSlaveExtraInfoKeyValue,$env:COMPUTERNAME,PoolName,$env:AZ_BATCH_POOL_ID
ExecuteDeadlineCommand -ArgumentList SetSlaveExtraInfoKeyValue,$env:COMPUTERNAME,ComputeNodeName,$env:AZ_BATCH_NODE_ID

if ('' -ne $deadlineGroups)
{
    # Deadline Groups
    $groups = $deadlineGroups.Replace(';', ',')
    Write-Output "Adding slave to groups $groups"
    ExecuteDeadlineCommand -ArgumentList SetGroupsForSlave,$env:COMPUTERNAME,$groups
}

if ('' -ne $deadlinePools)
{
    # Deadline Pools
    $pools = $deadlinePools.Replace(';', ',')
    Write-Output "Adding slave to pools $pools"
    ExecuteDeadlineCommand -ArgumentList SetPoolsForSlave,$env:COMPUTERNAME,$pools
}

if ('' -ne $excludeFromLimitGroups)
{
    # Deadline limit lists
    (New-Object System.Net.WebClient).DownloadFile('https://raw.githubusercontent.com/Azure/azure-deadline/master/CloudProviderPlugin/Scripts/limitgroups.py', 'limitgroups.py')
    $tokens = $excludeFromLimitGroups.Split(";")
    $tokens | ForEach {
            $limitGroup = $_
            Write-Output "Excluding slave from limit group $limitGroup"
            ExecuteDeadlineCommand -ArgumentList '-ExecuteScriptNoGui','limitgroups.py','--limitgroups',$limitgroup,'--slave',$env:COMPUTERNAME,'--exclude'
    }
}

# App insights support
if ($env:APP_INSIGHTS_APP_ID -and $env:APP_INSIGHTS_INSTRUMENTATION_KEY)
{
    # Install Batch Insights
    iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/Azure/batch-insights/master/scripts/run-windows.ps1')) | Out-File batchinsights.log
}

if (!$deadlineRunningAsService)
{
    # The Deadline slave isn't running as a service so we need to block
    # to keep the process alive.
    while($true)
    {
        Start-Sleep -Seconds 86400
    }
}
