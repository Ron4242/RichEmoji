param(
    [string]$Version,
    [string]$Author,
    [string]$Title,
    [string]$Description,
    [string]$ProjectUrl = "",
    [string]$OutDir,
    [string]$Dependencies = ""
)

$PackageDir = Join-Path $OutDir "package"
$ZipPath = Join-Path $OutDir "$Title`_v$Version.zip"

Remove-Item -Recurse -Force $PackageDir -ErrorAction SilentlyContinue
Remove-Item -Force $ZipPath -ErrorAction SilentlyContinue

New-Item -ItemType Directory -Path $PackageDir | Out-Null

$PluginDir = Join-Path $PackageDir "BepInEx\plugins"
New-Item -ItemType Directory -Path $PluginDir | Out-Null
Copy-Item -Recurse "$OutDir\*" $PluginDir -Exclude "package"

Copy-Item "Package\Thunderstore\icon.png" $PackageDir
Copy-Item "Package\Thunderstore\README.md" $PackageDir
Copy-Item "Package\Thunderstore\CHANGELOG.md" $PackageDir

$manifest = @{
    name = $Title
    version_number = $Version
    website_url = $ProjectUrl
    author = $Author
    description = $Description
    dependencies = @(if ($Dependencies)
    {
        $Dependencies -split ','
    }
    else
    {
        @()
    })
} | ConvertTo-Json

$manifest | Out-File -FilePath "$PackageDir\manifest.json" -Encoding utf8

Compress-Archive -Path "$PackageDir\*" -DestinationPath $ZipPath
Remove-Item -Recurse -Force $PackageDir