Write-Host "Before getting subscriptions, clear folder %appdata%\Windows Azure Powershell\*"
$azureps = $env:APPDATA + '\Windows Azure Powershell\*'
Write-Host "Removing folder" + $azureps
rm $azureps
Write-Host "Removed appdata windows azure powershell folder"

$AzureCertificateThumbPrint = $OctopusParameters['Deployment.Azure.CertificateThumbprint']
$AzureSubscriptionName = $OctopusParameters['Deployment.Azure.SubscriptionName']
$AzureSubscriptionId = $OctopusParameters['Deployment.Azure.SubscriptionId']
$AzureWebsiteName = $OctopusParameters['Deployment.Azure.WebsiteName']
$WebPackageName = $OctopusParameters['Deployment.Azure.WebPackageName']
$WebPackagePath = $OctopusParameters['Octopus.Action.Package.CustomInstallationDirectory'] + '\' + $WebPackageName
Write-Host "Web Package Path: " + $WebPackagePath

Write-Host "Getting azure subscriptions"
$subscriptions = Get-AzureSubscription
if(!$subscriptions)
{
	Write-Host "Looking for certificate in CurrentUser"
    $cert = dir cert:\CurrentUser  -rec | where { $_.Thumbprint -eq $AzureCertificateThumbPrint } | Select -First 1
    if(!$cert)
    {
        Write-Host "Not Found in CurrentUser. Looking at LocalMachine"
        $cert = dir cert:\LocalMachine  -rec | where { $_.Thumbprint -eq $AzureCertificateThumbPrint } | Select -First 1
    }
    
    if(!$cert)
    {
        throw "Certificate is not found in CurrentUser or LocalMachine"
    }
    
    Write-Host "Certificate was found. Setting azure subscription using the certificate..."
    Set-AzureSubscription -SubscriptionName '$AzureSubscriptionName' -Certificate $cert -SubscriptionId $AzureSubscriptionId
	Write-Host "Azure subscription was set successfully using the certificate obtained."
}
else
{
    Write-Host "Azure Subscriptions already exist"
    Write-Host $subscriptions
}
Write-Host "Selecting current azure subscription..."
Select-AzureSubscription -SubscriptionName '$AzureSubscriptionName' -Current
Write-Host "Selected current azure subscription. Publishing azure website..."
Publish-AzureWebsiteProject -Name $AzureWebsiteName -Package $WebPackagePath -Slot staging
Write-Host "Published azure website successfully."
