param([switch] $NoDebugger, [switch] $Verify)
$Invocation = (Get-Variable MyInvocation -Scope 0).Value
$ScriptPath = Split-Path $Invocation.MyCommand.Path

#cd $ScriptPath
Push-Location $ScriptPath
if($NoDebugger) {
	msbuild PostEdge.Tests.csproj /T:Rebuild
} else {
	msbuild PostEdge.Tests.csproj /T:Rebuild /P:PostsharpAttachDebugger=True
}


if($Verify){
	$originalBackground = $HOST.UI.RawUI.BackgroundColor
	$originalForeground = $HOST.UI.RawUI.ForegroundColor
	$HOST.UI.RawUI.BackgroundColor = "Black"
	$HOST.UI.RawUI.ForegroundColor = "Green"
	peverify "bin\Debug\PostEdge.Tests.dll"
	$HOST.UI.RawUI.BackgroundColor = $originalBackground
	$HOST.UI.RawUI.ForegroundColor = $originalForeground
}
Pop-Location
