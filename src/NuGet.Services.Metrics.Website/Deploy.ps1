$AzureSubscriptionName = $OctopusParameters['Deployment.Azure.SubscriptionName']
Select-AzureSubscription -SubscriptionName '$AzureSubscriptionName' -Current
Publish-AzureWebsiteProject -Name $DeploymentAzureWebsiteName -Package $OctopusActionPackageCustomInstallationDirectory -Slot staging