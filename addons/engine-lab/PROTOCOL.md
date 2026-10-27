# Local training protocol v1

Launch `ChessOpeningTrainer.exe --training-host`. Send one UTF-8 JSON object per stdin line; read exactly one JSON response per stdout line. No network server or credentials are involved. EOF shuts down the host and any teacher engine.

Replies are `{ "ok": true, "result": ... }` or `{ "ok": false, "error": "..." }`. Do not pipeline conflicting requests. Maximum 4,096 live games; requests larger than 8 MB are rejected.

| Operation | Fields | Result |
| --- | --- | --- |
| `hello` | none | `protocol`, `actions`, `stockfish` availability |
| `reset` | `id`, optional `fen`, `maxMoves` (full moves, default 500), `randomPlies` (0–300), `seed` | Initial state; generated positions reset measured move/repetition history |
| `state` | `id` | Current state |
| `step` | `id`, `move` (UCI) | `accepted`, `state`; illegal moves leave state unchanged |
| `remove` | `id` | Removes the instance |
| `analyze` | `id` or `fen`, `depth` (1–64), `multi` (1–10) | `lines`: `move`, `cp`, `mate`, `depth`, `pv`; raw UCI evaluations are positive for the side to move in the analyzed position (convert explicitly for a White-positive display) |
| `dataset_sample` | `file` (local dataset index), `seed`, `plies` (0–300) | A legal FEN sampled by walking the built local dataset; no mutation or download |
| `batch` | `items` array of operations | Ordered response array; no nested batches |

State includes `id`, complete `fen`, `board` (64 characters, a1 first, `.` empty), `white`, `castling`, `plies`, `status`, `result`, `legal`. Legal entries contain `uci` and `index`. Terminal states have no legal entries. Status is `playing`, `checkmate`, `draw` or `move_limit`. Results use PGN strings `1-0`, `0-1`, `1/2-1/2`, `*`.

Policy action: `(source * 64 + destination) * 5 + promotion`, where squares are zero-based a1..h8 and promotion is 0 none, 1 queen, 2 rook, 3 bishop, 4 knight. Check/capture annotations are unnecessary. Never infer legality from logits; use the supplied legal actions. An application-side cancellation stops pending engine searches on disposal.

```json
{"op":"reset","id":"game-1","maxMoves":500}
{"op":"step","id":"game-1","move":"e2e4"}
{"op":"analyze","id":"game-1","depth":4,"multi":3}
{"op":"remove","id":"game-1"}
```
