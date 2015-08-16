# Azure Subscription
$azureSubscriptionId="0e137dc5-6586-40f3-a4db-7ca19d781c98"

# Storage Account Name
$storageAccountName="cariotstorage"

# Cloud Services Name
$serviceName="cariotservice"

# Cloud Services and Service Bus Location. Choose a location that also has Stream Analytics and Machine Learning
$serviceLocation="South Central US"

# Service Bus Namespace
$serviceBusNamespace="cariotsb"

# Event Hub shared access policy key name
$eventHubSharedAccessPolicyKeyName="carsender"

# Event Hub shared access policy TTL in minutes (10 years)
$eventHubSharedAccessPolicyTTL="5259490"

# Package URL
$package_url="https://[your storage account].blob.core.windows.net/apppublish/package.cspkg?sv="

# Config URL
$config_Url="https://[your storage account].blob.core.windows.net/apppublish/config.cscfg?sv="

# Deploy in training mode?
$isTrainingMode = "true"

# Constants. Do not change.
# Cloud Services Slot
$slot="Production"
$configurationTableName="GeoPredictionConfiguration"
$trainingTableName="TrainingTable"

# Input event hub
$inputEventHubName="InputEventHub"
$inputEventHubPartitionCount = 32
$inputEventHubMessageRetentionInDays = 3

$outputEventHubName="OutputEventHub"
$outputEventHubPartitionCount = 32
$outputEventHubMessageRetentionInDays = 3

$consumerGroupName="ConsumerGroup"