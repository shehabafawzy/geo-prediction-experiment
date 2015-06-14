Write-Host -ForegroundColor Green -BackgroundColor Black "Loading setup parameters from"
Write-Host -ForegroundColor Blue -BackgroundColor White "$PSScriptRoot\SetupParameters.ps1"

# Include the setup parameters file. Make changes there to set your credentials, etc.
# Rename SetupParameters_template.ps1 to SetupParameters.ps1 for this script to work
. "$PSScriptRoot\SetupParameters.ps1"

Function InsertConfig($accountName,$accountKey,$tableName, $PartitionKey,$RowKey,$value   )
{
    Write-Host -ForegroundColor Blue -BackgroundColor White "$RowKey : $value"

    $Ctx = New-AzureStorageContext $accountName -StorageAccountKey $accountKey.Primary
    # Retrieve the table if it already exists.
    $table = Get-AzureStorageTable –Name $tableName -Context $Ctx -ErrorAction Ignore

    # Create a new table if it does not exist.
    if ($table -eq $null)
    {
       $table = New-AzureStorageTable –Name $tableName -Context $Ctx
    }

    # Create entity
    $entity = New-Object Microsoft.WindowsAzure.Storage.Table.DynamicTableEntity $PartitionKey, $RowKey
    $entity.Properties.Add("ConfigurationValue", $value)

    # Insert
    $result = $table.CloudTable.Execute([Microsoft.WindowsAzure.Storage.Table.TableOperation]::InsertOrReplace($entity))
}

Function Create-Deployment($package_url, $service, $slot, $config){
	$opstat = New-AzureDeployment -Slot $slot -Package $package_url -Configuration $config -ServiceName $service -Label "GeoPrediction" 
}
  
Function Upgrade-Deployment($package_url, $service, $slot, $config){
	$setdeployment = Set-AzureDeployment -Upgrade -Slot $slot -Package $package_url -Configuration $config -ServiceName $service -Force
}
 
Function Check-Deployment($service, $slot){
	$completeDeployment = Get-AzureDeployment -ServiceName $service -Slot $slot
	$completeDeployment.deploymentid
}

Function GetConfig($_configSource, $_storageConnectionString) {
	
	$invocation = (Get-Variable MyInvocation).Value
	$localPath=$invocation.InvocationName.Substring(0,$invocation.InvocationName.IndexOf($invocation.MyCommand))
	$configFile=$localPath +"ServiceConfiguration.Cloud.cscfg"
	
	If (Test-Path $configFile){
		Remove-Item $configFile
	}

	Invoke-WebRequest $_configSource -OutFile $configFile 

	 [xml]$configXml =Get-Content $configFile
	 $configXml.ServiceConfiguration.Role[0].ConfigurationSettings.Setting[0].value=$_storageConnectionString
  
	 $configXml.Save($configFile)

	 return $configFile
}

Function DeployService($_serviceName,$_slot,$_package_url,$_serviceLocation,$_config_Url,$_sExternalConnString){


	$config = GetConfig -_configSource $_config_Url -_storageConnectionString $_sExternalConnString
	# Cloud Services
	# check for existence
	$cloudService = Get-AzureService -ServiceName $_serviceName -ErrorVariable errPrimaryService -Verbose:$false -ErrorAction "SilentlyContinue"
	if ($cloudService -eq $null){
		# Create New CLoud Services
		New-AzureService -ServiceName $_serviceName -Location $_serviceLocation -ErrorVariable errPrimaryService -Verbose:$false 
					# -ErrorAction "SilentlyContinue" | Out-Null
	}

	# Get Deployment Data
	$deployment = Get-AzureDeployment -ServiceName $_serviceName -Slot $_slot -ErrorAction silentlycontinue
	if ($deployment.Name -eq $null) {
			Write-Host "No deployment is detected. Creating a new deployment. "
			Create-Deployment -package_url $_package_url -service $_serviceName -slot $_slot -config $config 
			Write-Host "New Deployment created"
 
		} else {
			Write-Host "Deployment exists in $service.  Upgrading deployment."
			Upgrade-Deployment -package_url $_package_url -service $_serviceName -slot $_slot -config $config
			Write-Host "Upgraded Deployment"
		}
	$deploymentid = Check-Deployment -service $_serviceName -slot $_slot
	Write-Host "Deployed to $_serviceName with deployment id $deploymentid"

	Remove-Item  $config
}


try
{

	# Setup
	# Subscription
    Write-Host -ForegroundColor Green -BackgroundColor Black "Using Azure Subscription $azureSubscriptionId"    
	Select-AzureSubscription -SubscriptionId  $azureSubscriptionId

	# Storage
    Write-Host -ForegroundColor Green -BackgroundColor Black "Checking for Storage Account $storageAccountName"    
	$newStorage = Get-AzureStorageAccount -StorageAccountName $storageAccountName
	if ($newStorage -eq $null) {
        Write-Host -ForegroundColor Green -BackgroundColor Black "Creating Storage Account $storageAccountName"            
		New-AzureStorageAccount -StorageAccountName $storageAccountName  -Location $serviceLocation -Type "Standard_LRS"
	}
    
    Write-Host -ForegroundColor Green -BackgroundColor Black "Setting Default Storage Account for subscription as $storageAccountName"            
	Set-AzureSubscription -SubscriptionId $azureSubscriptionId  -CurrentStorageAccountName $storageAccountName
	
	# Create the AuthorizationPolicy for the Event Hub
	$authRule = .\CreateSharedAccessAuthorizationRule -SharedAccessPolicyKeyName $eventHubSharedAccessPolicyKeyName

	# Create Service Bus Namespace and Event Hub
    Write-Host -ForegroundColor Green -BackgroundColor Black "Creating Input Event Hub $inputEventHubName in Service Bus Namespace $serviceBusNamespace"                
	.\CreateEventHub -AuthorizationRule $authRule -Path $inputEventHubName -PartitionCount $inputEventHubPartitionCount -MessageRetentionInDays $inputEventHubMessageRetentionInDays -UserMetadata 'This event hub is used by the devices of the IoT solution' -ConsumerGroupName $consumerGroupName -ConsumerGroupUserMetadata 'This consumer group is used by the IoT solution' -Namespace $serviceBusNamespace -Location $serviceLocation
	
    Write-Host -ForegroundColor Green -BackgroundColor Black "Creating Output Event Hub $outputEventHubName in Service Bus Namespace $serviceBusNamespace"                          
    .\CreateEventHub -AuthorizationRule $authRule -Path $outputEventHubName -PartitionCount $outputEventHubPartitionCount -MessageRetentionInDays $outputEventHubMessageRetentionInDays -UserMetadata 'This event hub is used by stream analytics of the IoT solution' -ConsumerGroupName $consumerGroupName -ConsumerGroupUserMetadata 'This consumer group is used the stream analytics of the IoT solution' -Namespace $serviceBusNamespace -Location $serviceLocation

	# Get the connection string of the service bus
	$serviceBusConnectionString = (Get-AzureSBNamespace -Name $serviceBusNamespace).ConnectionString

	# Create Configuration Table
    Write-Host -ForegroundColor Green -BackgroundColor Black "Checking for configuration table $configurationTableName"
	$sKey=Get-AzureStorageKey -StorageAccountName $storageAccountName
	$sExternalConnString='DefaultEndpointsProtocol=https;AccountName=' + $storageAccountName +';AccountKey='+ $sKey.Primary +''
	$storageContext= New-AzureStorageContext -StorageAccountKey $skey.Primary -StorageAccountName $storageAccountName
	 
    $confTable = Get-AzureStorageTable -Name $configurationTableName
    if ($confTable -eq $null) {
        Write-Host -ForegroundColor Green -BackgroundColor Black "Creating configuration table"            
	    New-AzureStorageTable -Context $storageContext -Name $configurationTableName
    }


	# Create Training Table
	Write-Host -ForegroundColor Green -BackgroundColor Black "Checking for training table $trainingTableName"
    $trTable = Get-AzureStorageTable -Name $trainingTableName
    if ($trTable -eq $null) {
        Write-Host -ForegroundColor Green -BackgroundColor Black "Creating training table"            
	    New-AzureStorageTable -Context $storageContext -Name $trainingTableName
    }

	# Insert Config Data
    Write-Host -ForegroundColor Green -BackgroundColor Black "Setting configuration data"            
	InsertConfig -PartitionKey "general" -RowKey "IsTrainingMode" -value $isTrainingMode -accountName $storageAccountName -accountKey $sKey -tableName $configurationTableName
	InsertConfig -PartitionKey "general" -RowKey "InputEventHubName" -value $inputEventHubName -accountName $storageAccountName -accountKey $sKey -tableName $configurationTableName 
	InsertConfig -PartitionKey "general" -RowKey "OutputEventHubName" -value $outputEventHubName -accountName $storageAccountName -accountKey $sKey -tableName $configurationTableName  
	InsertConfig -PartitionKey "general" -RowKey "ConsumerGroupName" -value $consumerGroupName -accountName $storageAccountName -accountKey $sKey -tableName $configurationTableName  
	InsertConfig -PartitionKey "general" -RowKey "ServiceBusNamespace" -value $serviceBusNamespace -accountName $storageAccountName -accountKey $sKey -tableName $configurationTableName  
	InsertConfig -PartitionKey "general" -RowKey "ServiceBusConnectionString" -value $serviceBusConnectionString -accountName $storageAccountName -accountKey $sKey -tableName $configurationTableName  
	InsertConfig -PartitionKey "general" -RowKey "StorageConnectionString" -value $sExternalConnString -accountName $storageAccountName -accountKey $sKey -tableName $configurationTableName  
	InsertConfig -PartitionKey "general" -RowKey "TrainingTableName" -value $trainingTableName -accountName $storageAccountName -accountKey $sKey -tableName $configurationTableName  
	InsertConfig -PartitionKey "client" -RowKey "EventHubSharedAccessPolicyKeyName" -value $eventHubSharedAccessPolicyKeyName -accountName $storageAccountName -accountKey $sKey -tableName $configurationTableName  
	InsertConfig -PartitionKey "client" -RowKey "EventHubSharedAccessPolicyKey" -value $authRule.PrimaryKey -accountName $storageAccountName -accountKey $sKey -tableName $configurationTableName  
	InsertConfig -PartitionKey "client" -RowKey "EventHubSharedAccessPolicyTTL" -value $eventHubSharedAccessPolicyTTL -accountName $storageAccountName -accountKey $sKey -tableName $configurationTableName  


	# Deploy Cloud Services
    #Write-Host -ForegroundColor Blue -BackgroundColor White "Deploying or updgrading Cloud Service"            
	# DeployService -_serviceName $serviceName -_package_url $package_url -_slot $slot -_serviceLocation $serviceLocation -_config_Url $config_Url -_sExternalConnString $sExternalConnString
}
catch 
{
	$ErrorMessage = $_.Exception.Message
    Write-Host $_.Exception
	Write-Host  $ErrorMessage	
}
