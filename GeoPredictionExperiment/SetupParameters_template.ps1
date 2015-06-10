# Azure Subscription
$azureSubscriptionId="[your subscription id]"

# Storage Account Name
$storageAccountName="[your Storage AccountName]"

# Cloud Services Name
$serviceName="[you Cloud Service Name here]"

# Cloud Services and Service Bus Location. Choose a location that also has Stream Analytics and Machine Learning
$serviceLocation="South Central US"

# Service Bus Namespace
$serviceBusNamespace="[your Service Bus Namespace]"

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