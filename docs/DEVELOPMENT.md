# Development

## Toolchain

- Windows 11 x64
- .NET SDK 10.0.401 or a compatible servicing patch (global.json); runtime packages pinned to 10.0.12 for the prepared release
- PowerShell 7
- Stockfish 19 from the pinned download script

Run tools/Get-Stockfish.ps1 before building. It verifies the archive and executable hashes and populates the ignored engine/stockfish_release directory. For a source build:

    dotnet run --project src/ChessOpeningTrainer -p:PublicRelease=true

PublicRelease enables clean example content and the one-time synthetic-game seed. It excludes the historical private example and non-GPL audio by default. Without the optional audio pack, synthesized sounds are used. A source build does not need Pixabay assets.

The curated repository sets PublicRelease=true by default in Directory.Build.props. The private development workspace keeps its previous default. The explicit flag in the command above works in either context.

Use CHESS_TRAINER_DATA to isolate development data. Never test with a personal profile. For the prepared Windows build use tools/Build-Release.ps1; see DISTRIBUTION.md.

## Boundaries

This remains one modular desktop assembly, not a new framework or service architecture. Domain rules, UCI engine services, storage and shared rendering have separate folders. See ARCHITECTURE.md. Existing serialized contracts and user data locations have not been renamed.

The private historical test harness has not been exported at the maintainer's request. CI_ASSESSMENT.md evaluates a later migration; there is no implicit test job or unsupported claim that tests run on GitHub.

## Custom assets

See src/ChessOpeningTrainer/Customization/README.md. Keep third-party assets separately licensed. Public documentation is English; UI dictionaries contain both languages. Do not package build artifacts, profiles or real games into source control.
