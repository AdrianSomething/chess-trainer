# Privacy and release-data boundary

The application stores progress, preferences, imported games and generated training locally, normally in %LOCALAPPDATA%\ChessOpeningTrainer. CHESS_TRAINER_DATA overrides this location for isolated testing. User opening files also live in the application's Openings folder.

No chess-site login, ChatGPT credential or cloud account is required. The application uses local engines and local training data, without a built-in account or telemetry service. Build scripts download dependencies; this is separate from playing and training in the installed application.

## Prepared public content

- Two maintainer-approved generated opening graphs, with private free-text metadata removed.
- One synthetic PGN containing only Example White and Example Black.
- Original/public customization assets and explicitly credited third-party components.
- No personal game library, learning progress, settings, real statistics, tokens, private logs, output folders or working archives.

The demo game is seeded only when the game-library file does not exist. Deleting the demo leaves a saved library and does not cause it to be re-added on every launch. A release does not erase an existing user's own games.

The 400-day statistics demo is opt-in in Chess Vision settings. Its generated data is display-only. A fresh package does not contain a saved demo profile.

Source exports and packages are allowlist-based. Build debug symbols are omitted and source-path mapping is used to avoid embedding the maintainer's absolute build path. Before distributing modifications, inspect new files, asset metadata and screenshots again.
