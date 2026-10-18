# Windows installation and updates

## First run

Use Windows 11 on an x64 processor. Stockfish is CPU-based; more cores and RAM help heavy analysis, but a dedicated graphics card is not required. The release is self-contained and has no installer.

1. Visit https://github.com/AdrianSomething/chess-trainer/releases.
2. Select a published release and download its win-x64 ZIP.
3. Optionally verify its SHA-256 against SHA256SUMS.txt using PowerShell:

       Get-FileHash .\ChessOpeningTrainer-0.24.0-win-x64.zip -Algorithm SHA256

4. Choose Extract All in Explorer. Run ChessOpeningTrainer.exe from the extracted directory.
5. Keep the engine, customization, opening and other supporting files beside the executable.

Do not run from inside the ZIP or copy only the EXE. GitHub's automatic source archives are for developers, not ready-to-run installations. This release is unsigned: review the origin if SmartScreen warns you; do not disable antivirus globally.

## What appears initially

The opening library contains the English Opening (White) and Pirc Defense (Black) examples. Your game library contains one clearly synthetic, deletable game: 1. e4 f6 2. d4 g5 3. Qh5#. Black cannot be checkmated before White's third move from the normal starting position.

English is the fresh-profile default. Existing preferences, including German, are preserved. The optional 400-day statistics demo is in Settings → Chess Vision, off by default, and never writes fabricated runs into your real history.

## Data and updates

Personal data normally lives in %LOCALAPPDATA%\ChessOpeningTrainer. Openings in the application folder are separate files. Back up both before an update. Extract a newer release into a new directory; do not overwrite a running copy. Copy your custom opening files deliberately and avoid replacing new bundled examples with older files.

Existing local profiles are not erased by a clean package. For a completely separate demonstration, use a temporary CHESS_TRAINER_DATA directory before launching:

    $env:CHESS_TRAINER_DATA = Join-Path $env:TEMP 'ChessTrainer-demo'
    .\ChessOpeningTrainer.exe

Close the app before switching profiles. Removing the application directory uninstalls the portable program; personal data remains unless you intentionally delete it.

## Troubleshooting

A missing engine usually means incomplete extraction or a security tool quarantined it. Check the original release and notices before restoring anything. Reduce worker counts and search depths if background training uses too much CPU. Never download an arbitrary replacement executable from an issue attachment.
