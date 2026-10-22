#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$OutputDirectory = '',
    [string]$CacheDirectory = '',
    [string]$PixabaySoundDirectory = ''
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'Publication.Files.ps1')
$project = [xml](Get-Content -LiteralPath (Join-Path $root 'src/ChessOpeningTrainer/ChessOpeningTrainer.csproj') -Raw)
$version = [string]($project.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1)
if ($version -notmatch '^\d+\.\d+\.\d+(-[A-Za-z0-9.]+)?$') { throw 'Invalid application version.' }
if (!$OutputDirectory) { $OutputDirectory = Join-Path $root "artifacts/$version" }
$out = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $out) { throw "Output already exists; nothing overwritten: $out" }
$engine = & (Join-Path $PSScriptRoot 'Get-Stockfish.ps1') -CacheDirectory $CacheDirectory -WithSource
New-Item -ItemType Directory -Path $out -Force | Out-Null
$app = Join-Path $out "ChessOpeningTrainer-$version-win-x64"
$companion = Join-Path $out "ChessOpeningTrainer-$version-corresponding-source"
New-Item -ItemType Directory -Path $companion -Force | Out-Null
# Compile from the same sanitized source that will accompany the binary.
$buildSource = Join-Path $companion 'application'
Copy-PublicSource $root $buildSource
$engineTarget = Join-Path $buildSource 'src/ChessOpeningTrainer/engine/stockfish_release/stockfish'
New-Item -ItemType Directory -Path $engineTarget -Force | Out-Null
foreach ($name in @('stockfish-windows-x86-64-universal.exe','Copying.txt','AUTHORS','README.md')) {
    Copy-Item -LiteralPath (Join-Path (Split-Path $engine.Binary -Parent) $name) -Destination (Join-Path $engineTarget $name)
}
$audioIncluded = ![string]::IsNullOrWhiteSpace($PixabaySoundDirectory)
if ($audioIncluded) {
    $audioManifest = Get-Content -LiteralPath (Join-Path $root 'third_party/pixabay-assets.json') -Raw | ConvertFrom-Json
    foreach ($asset in $audioManifest.assets) {
        $file = Join-Path $PixabaySoundDirectory $asset.file
        if ((Get-FileHash -LiteralPath $file).Hash -ne $asset.sha256) { throw "Unverified audio asset: $($asset.file)" }
    }
}
$previousCliHome = $env:DOTNET_CLI_HOME
$env:DOTNET_CLI_HOME = Join-Path $root '.dotnet-home'
try {
    $project = Join-Path $buildSource 'src/ChessOpeningTrainer/ChessOpeningTrainer.csproj'
    $arguments = @('publish', $project, '-c','Release','-r','win-x64','--self-contained','true',
        '-p:PublishSingleFile=true','-p:PublicRelease=true','-p:RuntimeFrameworkVersion=10.0.12',
        '-p:UseSharedCompilation=false','-p:DebugType=none','-p:DebugSymbols=false',
        "-p:PathMap=$buildSource=/_/chess-trainer",'-o',$app,'-v:minimal')
    if ($audioIncluded) { $arguments += '-p:IncludePixabaySounds=true' }
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
}
finally { $env:DOTNET_CLI_HOME = $previousCliHome }
if ($audioIncluded) {
    $audioTarget = Join-Path $app 'Customization/sounds/user'
    New-Item -ItemType Directory -Path $audioTarget -Force | Out-Null
    foreach ($asset in $audioManifest.assets) {
        Copy-Item -LiteralPath (Join-Path $PixabaySoundDirectory $asset.file) -Destination (Join-Path $audioTarget $asset.file)
    }
    # Generate the known manifest instead of copying arbitrary user metadata.
    $pack = @{ id='user'; name='Provided sounds'; files=@{
        light=@('Piece_04_light.mp3','Piece_05_light.mp3')
        heavy=@('Piece_01_heavy.mp3','Piece_02_heavy.mp3','Piece_03_heavy.mp3')
        check=@('Check.mp3'); mate=@('Checkmate.mp3'); success=@('Confirmarion.mp3'); error=@('Error.mp3')
    }}
    $pack | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $audioTarget 'pack.json') -Encoding utf8
}
# Re-export the clean application source, without transient compiler/engine files.
# Do not delete user directories: move the build-only tree to an ignored artifact sibling.
$buildCache = [IO.Path]::GetFullPath((Join-Path $root '.cache/build'))
$buildOnly = Join-Path $buildCache ([Guid]::NewGuid().ToString('N'))
if (![IO.Path]::GetFullPath($buildSource).StartsWith($out + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    ![IO.Path]::GetFullPath($buildOnly).StartsWith($buildCache + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Build-intermediate paths must stay inside their intended release/cache directories.'
}
New-Item -ItemType Directory -Path $buildCache -Force | Out-Null
Move-Item -LiteralPath $buildSource -Destination $buildOnly
Copy-PublicSource $root $buildSource
$sfSource = Join-Path $companion 'stockfish'
Copy-Item -LiteralPath $engine.SourceDirectory -Destination $sfSource -Recurse
$networkName = Split-Path $engine.Network -Leaf
Copy-Item -LiteralPath $engine.Network -Destination (Join-Path $sfSource "src/$networkName")
foreach ($name in @('LICENSE','COPYRIGHT.md','THIRD_PARTY_NOTICES.md')) {
    Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $app $name)
}
New-Item -ItemType Directory -Path (Join-Path $app 'docs'), (Join-Path $app 'licenses/dotnet') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'docs/INSTALLATION.md') -Destination (Join-Path $app 'README.md')
foreach ($name in @('LICENSE_COMPLIANCE.md','ASSET_PROVENANCE.md','PRIVACY.md','RELEASE_VALIDATION.md','DATASETS.md')) {
    Copy-Item -LiteralPath (Join-Path $root "docs/$name") -Destination (Join-Path $app "docs/$name")
}
Get-ChildItem -LiteralPath (Join-Path $root 'third_party/dotnet') -File | Copy-Item -Destination (Join-Path $app 'licenses/dotnet')
Copy-Item -LiteralPath (Join-Path $root 'third_party') -Destination (Join-Path $app 'third_party') -Recurse
$metadata = [ordered]@{
    applicationVersion=$version; runtime='10.0.12'; target='win-x64'; publicRelease=$true
    includePixabaySounds=$audioIncluded; sourcePathMap='/_/chess-trainer'
    stockfishCommit='edb0d9db6731067ec50ce619ff372b463bc4dd5d'
    sourceCompanion="ChessOpeningTrainer-$version-corresponding-source.zip"
    signed=$false
}
$metadata | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $app 'release.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $app 'release.json') -Destination (Join-Path $companion 'release.json')
@'
# Corresponding source

application/ contains the exact curated application source and packaging scripts.
stockfish/ contains the full pinned Stockfish source, its build recipes and the
unchanged NNUE file in src/. Read application/docs/LICENSE_COMPLIANCE.md.

The complete Windows edition sets PublicRelease=true and IncludePixabaySounds=true.
The latter changes the default sound-pack selection only; the source build without
that separately licensed pack uses synthesized sounds. Edited Pixabay MP3 data is
not GPL program source and is deliberately not offered as a standalone collection.

Use the included scripts on Windows with .NET 10 SDK / PowerShell 7 to rebuild.
No bit-identical compiler/toolchain reproducibility is claimed.
'@ | Set-Content -LiteralPath (Join-Path $companion 'README.md') -Encoding utf8
& (Join-Path $PSScriptRoot 'Audit-Publication.ps1') -Directory $buildSource
& (Join-Path $PSScriptRoot 'Audit-Publication.ps1') -Directory $app
Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach ($folder in @($app,$companion)) {
    [IO.Compression.ZipFile]::CreateFromDirectory($folder, "$folder.zip", [IO.Compression.CompressionLevel]::Optimal, $true)
}
Get-ChildItem -LiteralPath $out -Filter '*.zip' -File | Sort-Object Name | ForEach-Object {
    "$((Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant())  $($_.Name)"
} | Set-Content -LiteralPath (Join-Path $out 'SHA256SUMS.txt') -Encoding ascii
Write-Output "Prepared release artifacts: $out (no upload performed)"
