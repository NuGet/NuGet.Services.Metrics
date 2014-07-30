Write-Host "Trying to find Certificate with Thumbprint:"
Write-Host '$DeploymentAzureCertificateThumbprint'
$cert = dir cert:\LocalMachine  -rec | where { $_.Thumbprint -eq '$DeploymentAzureCertificateThumbprint' } | Select -First 1
if(!$cert)
{
    Write-Host "Not Found in LocalMachine"
    $cert = dir cert:\CurrentUser  -rec | where { $_.Thumbprint -eq '$DeploymentAzureCertificateThumbprint' } | Select -First 1
}

if(!$cert)
{
    throw "Certificate is not found"
}

Set-AzureSubscription -SubscriptionName '$DeploymentAzureSubscriptionName' -Certificate $cert -SubscriptionId $DeploymentAzureSubscriptionId
Select-AzureSubscription -SubscriptionName '$DeploymentAzureSubscriptionName' -Current
Publish-AzureWebsiteProject -Name $DeploymentAzureWebsiteName -Package $OctopusActionPackageCustomInstallationDirectory -Slot staging