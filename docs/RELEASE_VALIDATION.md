# Validation record — 0.24.0

Validation date: 2026-10-18. These are local checks, not GitHub CI or an independent audit.

## Functional baseline

The preceding publication preparation completed the local rules/schema checks, 1,033 UI/regression PASS records and 127 real-engine PASS records. The public-source changes for 0.24.0 concern version metadata, documentation, supplied screenshots and prompt-record privacy; no chess behavior is intentionally changed.

Both bundled graphs passed the application's legality/reference validator: 714 learner cards for the English example and 383 for the Pirc example. Fresh-profile checks verified English defaults, exactly one synthetic game, persistent demo deletion, preservation of existing libraries and an off-by-default statistics demo in Chess Vision settings.

## Publication checks

Source and release exports exclude private profiles, real PGN archives, build logs and historical work folders. Credential/path pattern scanning complements an explicit file selection. The maintainer's supplied screenshots were visually reviewed; screenshot text/EXIF metadata is omitted from the public copies. PROMPTS.md preserves historical German prompts with identifying crash paths and report identifiers redacted.

Stockfish's binary, pinned source and embedded network were verified as documented in LICENSE_COMPLIANCE.md. No executable is tracked in this source repository.

## Limitations

The previous build initially could not reach NuGet vulnerability information; a subsequent package audit reached nuget.org and reported no vulnerable packages. This project has no ordinary third-party PackageReferences; that result does not audit native Stockfish, Windows or every runtime component.

Tests cannot prove every interaction, training recommendation or hardware combination. The regression harness remains outside this repository pending a separate CI/test migration. Releases are unsigned. No independent bit-identical rebuild or blanket legal guarantee is claimed.

Original editable Figma documents and audio download certificates/dates were not supplied; their provenance and licensing boundaries are documented rather than reconstructed.
