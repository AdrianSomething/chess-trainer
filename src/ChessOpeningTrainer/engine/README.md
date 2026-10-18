# Local Stockfish engine

Run tools/Get-Stockfish.ps1 from the repository root to fetch the pinned Stockfish 19 Windows x64 universal build. Downloaded binaries are ignored by Git. The script verifies hashes from third_party/stockfish.json.

The application uses UCI over a local process. No GPU or remote inference account is required. Only use trusted executables. Distribution must retain the GPL license, authorship and matching corresponding source/network; see docs/LICENSE_COMPLIANCE.md and THIRD_PARTY_NOTICES.md in the repository.

Release packages contain the engine; end users do not need to fetch it separately.
