[CmdletBinding()]
param(
    [Parameter(Mandatory, Position=0)]
    [string]$ServiceRoot,
	[Parameter(Mandatory, Position=1)]
	[string]$TestAssembly,
    [Parameter(Position=2)]
    [switch]$TeamCity)

Write-Host "Finding xunit runner"
$list = @(dir xunit.console.exe -Recurse)
if($list.Count -eq 0)
{
	throw "Xunit console exe is not found"
}

$xunitexe = $list[0].FullName
$env:NUGET_TEST_SERVICEROOT=$ServiceRoot
if($TeamCity)
{
    &"$xunitexe" $TestAssembly -parallel all -teamcity
}
else
{
    &"$xunitexe" $TestAssembly -parallel all  
}