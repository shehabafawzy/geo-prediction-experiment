[CmdletBinding(PositionalBinding=$True)]
Param(
    [Parameter(Mandatory = $true)]
    [String]$SharedAccessPolicyKeyName
)


$scriptPath = Split-Path (Get-Variable MyInvocation -Scope 0).Value.MyCommand.Path
$packagesFolder = $scriptPath + "\packages"
$assembly = Get-ChildItem $packagesFolder -Include "Microsoft.ServiceBus.dll" -Recurse
Add-Type -Path $assembly.FullName

$AuthorizationKey = [Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule]::GenerateRandomKey()
$AccessRights = [Microsoft.ServiceBus.Messaging.AccessRights[]]([Microsoft.ServiceBus.Messaging.AccessRights]::Send)
$AuthrorizationRule = New-Object -TypeName Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule ($SharedAccessPolicyKeyName,$AuthorizationKey,$AccessRights)
	
return $AuthrorizationRule