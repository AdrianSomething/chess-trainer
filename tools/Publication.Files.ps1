# Shared allowlist for a clean source export. Never recurse into private work/profile data.
function Copy-PublicSource([string]$From, [string]$To) {
    $From = [IO.Path]::GetFullPath($From)
    $To = [IO.Path]::GetFullPath($To)
    if (Test-Path -LiteralPath $To) { throw "Source destination already exists: $To" }
    New-Item -ItemType Directory -Path $To -Force | Out-Null
    $roots = @('README.md','LICENSE','COPYRIGHT.md','CONTRIBUTING.md','CHANGELOG.md','SECURITY.md','THIRD_PARTY_NOTICES.md','CITATION.cff','.gitignore','.gitattributes','.editorconfig','global.json')
    $toolNames = @('Get-Stockfish.ps1','Build-Release.ps1','Publication.Files.ps1','Audit-Publication.ps1','Create-AppIcon.ps1','repository-defaults.props')
    $files = foreach ($name in $roots) { Get-Item -LiteralPath (Join-Path $From $name) -ErrorAction Stop }
    foreach ($folder in @('docs','.github','third_party','src/ChessOpeningTrainer')) {
        $files += Get-ChildItem -LiteralPath (Join-Path $From $folder) -File -Recurse | Where-Object {
            $rel = [IO.Path]::GetRelativePath($From, $_.FullName).Replace('\','/')
            $rel -notmatch '/(bin|obj|stockfish_release|versions|history)/' -and
            $rel -notmatch '/sounds/user/' -and
            $rel -notmatch '/(custom_|combined_)' -and
            $rel -notmatch '/example_pirc_defense.json$' -and
            $rel -notmatch '/(OPENING_FORMAT_DE|AI_BRIEFING_FUER_OPENING_RECHERCHE|GENERATOR_README|README_STOCKFISH_DE)\.md$' -and
            $rel -notmatch '^\.github/workflows/' -and
            $rel -notmatch '^docs/ai-development/.*_TEMPLATE\.md$' -and
            $rel -notmatch '^docs/(SCREENSHOTS|REPOSITORY_GUIDANCE)\.md$' -and
            $rel -notmatch '^docs/images/(home|learning)\.png$|^docs/images/feature-placeholder\.svg$' -and
            $_.Extension -in @('.cs','.csproj','.json','.md','.png','.svg','.ico','.wav','.pgn','.manifest','.txt','.yml','.props') 
        }
    }
    foreach ($name in $toolNames) { $files += Get-Item -LiteralPath (Join-Path $From "tools/$name") }
    foreach ($file in $files) {
        $rel = [IO.Path]::GetRelativePath($From, $file.FullName)
        $dest = Join-Path $To $rel
        New-Item -ItemType Directory -Path (Split-Path $dest -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $dest
    }
    $agentSource = Join-Path $From 'AGENTS.md'
    if (Test-Path -LiteralPath (Join-Path $From 'docs/REPOSITORY_GUIDANCE.md')) { $agentSource = Join-Path $From 'docs/REPOSITORY_GUIDANCE.md' }
    Copy-Item -LiteralPath $agentSource -Destination (Join-Path $To 'AGENTS.md')
    Copy-Item -LiteralPath (Join-Path $From 'tools/repository-defaults.props') -Destination (Join-Path $To 'Directory.Build.props')
}
