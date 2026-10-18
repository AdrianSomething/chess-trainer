# Local training preparation

The JSON files in this directory are project seed material for local training, not personal game data or a complete public chess database. Runtime generation, validation, buffers and completed-run records are separate from these read-only seeds.

Generation lives in Features/Puzzles/Generation, session validation in Features/Puzzles/Session, and registered views in Features/Puzzles/Tabs. Chess Vision has its own bounded preparation buffers in Features/Vision. Engines run locally; CPU worker limits are configurable.

Tasks can originate from compositions, local engine self-play, endgame generation or the user's own imported games. Different objectives use different validators. Short mate/endgame proofs and engine evaluations must not be conflated with universal mathematical guarantees. Randomness, filters and hardware can make refill times vary.

A fresh public package does not contain the maintainer's solved tasks or game-derived mistakes. Personal caches and statistics belong to the user profile. Do not package that profile when redistributing the application.
