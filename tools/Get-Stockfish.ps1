#requires -Version 7.0
[CmdletBinding()]
param([string]$CacheDirectory = '', [switch]$WithSource)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$lock = Get-Content -LiteralPath (Join-Path $root 'third_party/stockfish.json') -Raw | ConvertFrom-Json
if (!$CacheDirectory) { $CacheDirectory = Join-Path $root '.cache/stockfish' }
$cache = [IO.Path]::GetFullPath($CacheDirectory)
New-Item -ItemType Directory -Path $cache -Force | Out-Null
function Assert-Hash([string]$File, [string]$Expected) {
    if ((Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash -ne $Expected) { throw "SHA-256 mismatch: $File" }
}
function Fetch([string]$Url, [string]$Name, [string]$Hash) {
    $file = Join-Path $cache $Name
    if (!(Test-Path -LiteralPath $file)) { Invoke-WebRequest -Uri $Url -OutFile $file }
    Assert-Hash $file $Hash
    return $file
}
$zip = Fetch $lock.binaryUrl 'stockfish-19.zip' $lock.binaryArchiveSha256
$expanded = Join-Path $cache 'binary'
if (!(Test-Path -LiteralPath $expanded)) { Expand-Archive -LiteralPath $zip -DestinationPath $expanded }
$binary = Join-Path $expanded 'stockfish/stockfish-windows-x86-64-universal.exe'
Assert-Hash $binary $lock.executableSha256
$destination = Join-Path $root 'src/ChessOpeningTrainer/engine/stockfish_release/stockfish'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$installed = Join-Path $destination 'stockfish-windows-x86-64-universal.exe'
if (Test-Path -LiteralPath $installed) { Assert-Hash $installed $lock.executableSha256 }
else { Copy-Item -LiteralPath $binary -Destination $installed }
foreach ($name in @('Copying.txt','AUTHORS','README.md')) {
    Copy-Item -LiteralPath (Join-Path $expanded "stockfish/$name") -Destination (Join-Path $destination $name) -Force
}
$sourceDirectory = $null
$network = $null
if ($WithSource) {
    $sourceZip = Fetch $lock.sourceUrl 'stockfish-source.zip' $lock.sourceArchiveSha256
    $sourceExpanded = Join-Path $cache 'source'
    if (!(Test-Path -LiteralPath $sourceExpanded)) { Expand-Archive -LiteralPath $sourceZip -DestinationPath $sourceExpanded }
    $sourceDirectory = Join-Path $sourceExpanded ("Stockfish-" + $lock.commit)
    $network = Join-Path $cache $lock.network
    if (!(Test-Path -LiteralPath $network)) {
        Push-Location $cache
        try { @("export_net $($lock.network)",'quit') | & $binary | Out-Null }
        finally { Pop-Location }
    }
    Assert-Hash $network $lock.networkSha256
}
[pscustomobject]@{ Binary = $installed; SourceDirectory = $sourceDirectory; Network = $network }
