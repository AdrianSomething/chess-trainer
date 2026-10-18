#requires -Version 7.0
param([Parameter(Mandatory)][string]$Directory)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($Directory)
$files = @(Get-ChildItem -LiteralPath $root -Recurse -File -Force)
$problems = [Collections.Generic.List[string]]::new()
foreach ($file in $files) {
    $rel = [IO.Path]::GetRelativePath($root, $file.FullName).Replace('\','/')
    if ($rel -match '(^|/)(outputs|work|private|AppData|\.git|tests|bin|obj)/' -or $rel -match '^docs/(history|versions)/' -or
        $rel -match '(personal-games|preferences|progress|vision-statistics)\.json$' -or
        $rel -match '\.(pdb|user|suo|pem|pfx|key)$' -or $rel -match '(^|/)\.env($|\.)') {
        $problems.Add("Disallowed private/build file: $rel")
    }
    if ($file.Extension -eq '.pgn' -and $rel -notmatch '(^|/)Samples/demo-game\.pgn$') { $problems.Add("Unexpected PGN: $rel") }
    if ($rel -match 'example_pirc_defense\.json$|(^|/)(custom_|combined_).*\.json$') { $problems.Add("Unapproved opening: $rel") }
    if ($file.Extension -in @('.cs','.json','.md','.txt','.xml','.csproj','.props','.cff','.pgn','.yml')) {
        $content = [IO.File]::ReadAllText($file.FullName)
        if ($content -match '(?i)[A-Z]:\\Users\\(?!Public\\|Default\\)[^\\\s]+' -or
            $content -match '(gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}|sk-[A-Za-z0-9_-]{32,}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----)') {
            $problems.Add("Potential private path/credential in: $rel")
        }
    }
}
if ($problems.Count) { throw ($problems -join [Environment]::NewLine) }
Write-Output "Privacy structure/pattern audit passed: $($files.Count) files. This does not replace human review of new media/content."
