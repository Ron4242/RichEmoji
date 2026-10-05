$ErrorActionPreference = 'Stop'

function New-StagingDir([string]$Name)
{
    $dir = Join-Path ([System.IO.Path]::GetTempPath()) "$Name-package"
    Remove-Item -Recurse -Force $dir -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $dir | Out-Null
    return $dir
}

function Copy-PluginFiles([string]$OutDir, [string]$Destination)
{
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    Get-ChildItem $OutDir -Exclude "packages", "*.zip" | Copy-Item -Destination $Destination -Recurse
}

# no BOM
function Write-Json([string]$Path, $Object)
{
    $json = $Object | ConvertTo-Json
    [System.IO.File]::WriteAllText($Path, $json, (New-Object System.Text.UTF8Encoding $false))
}

function Split-Dependencies([string]$Dependencies)
{
    if ($Dependencies)
    {
        return ,@($Dependencies -split ',')
    }
    return ,@()
}

function Compress-Package([string]$StagingDir, [string]$OutDir, [string]$ZipName)
{
    $packagesDir = Join-Path $OutDir "packages"
    New-Item -ItemType Directory -Path $packagesDir -Force | Out-Null

    $zipPath = Join-Path $packagesDir $ZipName
    Remove-Item -Force $zipPath -ErrorAction SilentlyContinue
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $root = (Resolve-Path $StagingDir).Path.TrimEnd('\') + '\'
    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try
    {
        foreach ($file in Get-ChildItem $StagingDir -Recurse -File)
        {
            $entry = $file.FullName.Substring($root.Length).Replace('\', '/')
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, $entry, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally
    {
        $zip.Dispose()
    }

    Remove-Item -Recurse -Force $StagingDir

    Write-Host "Packaged $zipPath"
}
