param(
    [string]$Version,
    [string]$Author,
    [string]$Title,
    [string]$Description,
    [string]$ProjectUrl = "",
    [string]$OutDir,
    [string]$Dependencies = ""
)

. "$PSScriptRoot\..\common.ps1"

$staging = New-StagingDir "Nexus"

Copy-PluginFiles $OutDir (Join-Path $staging "BepInEx\plugins\$Author-$Title")

Compress-Package $staging $OutDir "$Title`_Nexus_v$Version.zip"
