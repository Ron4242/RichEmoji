# https://hexium.gg/packaging

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

$shared = "$PSScriptRoot\.."

if ($Description.Length -gt 256)
{
    throw "Hexium descriptions are limited to 256 characters"
}
if ($Title -notmatch '^[A-Za-z0-9_]{1,128}$')
{
    throw "Hexium names may only contain letters, digits and underscores"
}

$staging = New-StagingDir "Hexium"

Copy-PluginFiles $OutDir (Join-Path $staging "BepInEx\plugins")

Copy-Item "$shared\icon.png" $staging
Copy-Item "$shared\README.md" $staging
Copy-Item "$shared\CHANGELOG.md" $staging

Write-Json "$staging\manifest.json" ([ordered]@{
    name = $Title
    description = $Description
    version_number = $Version
    website_url = $ProjectUrl
    dependencies = (Split-Dependencies $Dependencies)
})

Compress-Package $staging $OutDir "$Title`_Hexium_v$Version.zip"
