# https://thunderstore.io/package/create/docs/

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

$staging = New-StagingDir "Thunderstore"

Copy-PluginFiles $OutDir (Join-Path $staging "BepInEx\plugins")

Copy-Item "$PSScriptRoot\..\icon.png" $staging
Copy-Item "$PSScriptRoot\..\README.md" $staging
Copy-Item "$PSScriptRoot\..\CHANGELOG.md" $staging

Write-Json "$staging\manifest.json" ([ordered]@{
    name = $Title
    version_number = $Version
    website_url = $ProjectUrl
    author = $Author
    description = $Description
    dependencies = (Split-Dependencies $Dependencies)
})

Compress-Package $staging $OutDir "$Title`_Thunderstore_v$Version.zip"
