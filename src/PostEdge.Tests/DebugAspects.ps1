$Invocation = (Get-Variable MyInvocation -Scope 0).Value
$ScriptPath = Split-Path $Invocation.MyCommand.Path

#cd $ScriptPath
Push-Location $ScriptPath
msbuild PostEdge.Tests.csproj /T:Rebuild /P:PostsharpAttachDebugger=True
Pop-Location
