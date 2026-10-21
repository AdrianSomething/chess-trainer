# Changelog

Earlier private iterations are not presented as independently published releases.

## 0.25.0-beta.1 — Development

- Added prioritized opening-generation jobs with shared engine workers, per-job approval and optional queue-wide pauses.
- Added optional work/rest intervals and a single multi-job graph preview.
- Added extraction of a selected variation, preserving its approach moves and the original repertoire.
- Moved initial branch graph preparation off the UI thread and reused legal-move lists per node.
- Added bounded layout caching and exact crossing-candidate pruning for faster repeated branch loading.
- Added home-menu bookmarks and persistent unsent generator drafts, with completed-job cleanup and direct learning/branch actions.
- Require one safe destination and a depth-10 engine quality check in Save a Piece exercises.
- Added an English/German guided first-run overview and a restart action in General settings.
- Embedded tutorial cards in their host windows, with live board updates, review navigation and keyboard-safe interaction steps.
- Removed synchronous audio-dispatch waits; defer playback until media loading completes and lower background engine process priority.
- Kept opening schemas and existing opening IDs compatible with 0.24.0.

## 0.24.0 — 2026-10-18

- Added English project, installation, development, privacy, contribution, security and license documentation.
- Prepared a curated source export and Windows package without uploading either.
- Recorded verification of Stockfish 19's binary, source and neural network.
- Separated Pixabay audio licensing from the GPL source and synthesized-sound fallback.
- Replaced the public legacy opening with two sanitized generated repertoires.
- Added a removable synthetic game for fresh public profiles.
- Moved the 400-day vision-statistics demo toggle to Chess Vision settings, off by default.
- Added the maintainer's retrospective prompt record and screenshots; removed unused documentation templates.
- Adopted the 0.24.0 development version number for the initial public source publication.
- Assessed future CI without exporting the private tests or enabling workflows.

Existing application feature architecture is unchanged.
