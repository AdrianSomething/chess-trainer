# Building and publishing releases

The source repository is distinct from runnable release assets. A GitHub source ZIP does not contain Stockfish or the self-contained .NET runtime.

## Build on Windows

Install the SDK selected by global.json and PowerShell 7, then run:

    pwsh -File tools/Get-Stockfish.ps1
    pwsh -File tools/Build-Release.ps1

Output defaults to artifacts/0.24.0:

- ChessOpeningTrainer-0.24.0-win-x64.zip: self-contained Windows application.
- ChessOpeningTrainer-0.24.0-corresponding-source.zip: application source, full pinned Stockfish source and its exact NNUE network.
- SHA256SUMS.txt: archive checksums.

By default, the build uses project-synthesized sounds. To build the complete audio edition, pass -PixabaySoundDirectory with the separately licensed local pack. Files must match third_party/pixabay-assets.json. That pack is excluded from this repository and must not be published as a standalone audio collection.

The script refuses to overwrite an existing output directory. Dependencies are hash-verified; intermediate files stay in ignored cache directories. No signing, GitHub upload or release creation is performed by the script.

## Publish

1. Review THIRD_PARTY_NOTICES.md, COPYRIGHT.md and LICENSE_COMPLIANCE.md.
2. Build and validate the exact source revision in a clean profile. Check sample data, screenshots, runtime notices and checksums.
3. Create a version tag and a GitHub Release referring to that source revision.
4. Upload the Windows ZIP, matching corresponding-source companion and checksum file together. GitHub's automatic source ZIP does not replace the companion because it omits the downloaded engine source/network.
5. Keep the corresponding source available with each distributed binary. Modified binaries require matching modified source and notices.

Release metadata must distinguish the synthesized-sound build from the complete audio edition. The latter contains non-GPL Pixabay media. Do not claim code signing or bit-identical rebuilds unless actually established.

Local regression evidence and its limitations are summarized in RELEASE_VALIDATION.md. Future CI integration is assessed in CI_ASSESSMENT.md; no workflow is enabled.
