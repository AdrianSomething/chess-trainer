# Third-party notices

Checked for release preparation on 2026-10-18. This inventory distinguishes code, runtime components and media; a project-wide GPL notice does not replace their individual terms.

## Stockfish 19

- Upstream: https://github.com/official-stockfish/Stockfish
- Release: https://github.com/official-stockfish/Stockfish/releases/tag/sf_19
- Commit: edb0d9db6731067ec50ce619ff372b463bc4dd5d
- License: GPL-3.0-or-later; preserve Copying.txt and AUTHORS.
- Binary: official Windows x86-64 universal executable, unmodified.
- Network: nn-1a298aa575a0.nnue, exported unchanged from that binary.

The Windows package contains Stockfish license/authorship notices. Its corresponding-source release companion contains the full pinned upstream tree, build scripts and network. See docs/LICENSE_COMPLIANCE.md for checksums and build details. Upstream also acknowledges Leela Chess Zero training data under ODbL; its notices are retained. No training dataset is distributed here.

## Microsoft .NET 10.0.12

The self-contained Windows package includes .NET runtime and Windows Desktop components from Microsoft runtime packs. Their MIT licenses and third-party notices are in licenses/dotnet/ in the Windows package. Preserve these files when redistributing. Package versions and provenance are recorded in release metadata.

Windows and installed system fonts are not included.

## Pixabay effects — complete Windows edition only

These files remain subject to the [Pixabay Content License](https://pixabay.com/service/terms/), not GPL. [The license summary](https://pixabay.com/service/license-summary/) permits modification and use without mandatory attribution, but prohibits standalone distribution. We nevertheless credit the sources.

| Use / packaged files | Source | Source credit |
| --- | --- | --- |
| Checkmate.mp3 | [Chess pieces hitting wooden board, 99336](https://pixabay.com/sound-effects/film-special-effects-chess-pieces-hitting-wooden-board-99336/) | XfiXy8 (Freesound), posted by freesound_community |
| Piece_01_heavy.mp3, Piece_02_heavy.mp3, Piece_03_heavy.mp3, Piece_04_light.mp3, Piece_05_light.mp3, Check.mp3 | [Chess pieces, 60890](https://pixabay.com/sound-effects/film-special-effects-chess-pieces-60890/) | simone_ds (Freesound), posted by freesound_community |
| Confirmarion.mp3 (existing filename) | [Confirm tap, 394001](https://pixabay.com/sound-effects/film-special-effects-confirm-tap-394001/) | ExistentialTaco; the source labels this effect AI-generated |
| Error.mp3 | [Error notification 08, 206492](https://pixabay.com/sound-effects/film-special-effects-error-notification-08-206492/) | Universfield |

The maintainer reports editing all these effects in Audacity. Original download dates and exact edit histories were not supplied. Source links do not prove authorship beyond the source pages' statements.

The effects are used as contextual application feedback in the complete Windows edition, not offered as a sound library. The public source export excludes this pack and defaults to the project's synthesized sounds. Do not treat the edited effects as GPL assets, extract them for a standalone collection, or assume that editing removes Pixabay's conditions. Their inclusion does not restrict the separate GPL program's code.

## Original graphics and tool acknowledgements

The maintainer reports creating Customization/icons/assessment-sheet.png with Figma. It is licensed separately under CC-BY-4.0; credit Chess Opening Trainer contributors and https://github.com/AdrianSomething/chess-trainer, preserve the license and indicate changes. Its supplied raster is unchanged; no editable Figma source was provided. The full license is in third_party/CC-BY-4.0.txt. Other project icons and silhouettes are drawn by GPL project code. No Figma software or community asset collection is bundled.

Audacity was used to edit audio; Audacity is not bundled. Its application license does not automatically license the audio it edits. Codex assisted development; no OpenAI credentials, proprietary runtime or remote model service is required to run this application.

See COPYRIGHT.md and docs/ASSET_PROVENANCE.md for scope and remaining provenance limitations.
