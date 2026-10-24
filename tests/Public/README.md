# Public synthetic checks

Run from the repository root on Windows with the SDK in `global.json`:

```powershell
dotnet run --project tests/Public -- rules
dotnet run --project tests/Public -- datasets
```

`RulesChecks` covers initial-position perft, castling, en passant, a terminal
position and all 20,480 packed source/target/promotion combinations.
`BoardColorChecks` renders off-screen and verifies all 64 square colors in both
orientations for every bundled skin; it does not require desktop automation.
`StorageChecks` covers synthetic transpositions, exact outcome aggregation,
cutoff measurement versus actual output, cancellation and forced-tail browsing.
Each invocation owns a unique `work/public-checks-*` profile and removes it.
There are no real player records, external network calls or Stockfish processes.

The small console runner exits nonzero on the first failed assertion. Thousands
of cheap assertions share one process; they are not thousands of CI jobs.
The larger local UI/engine harness remains private. These checks complement it;
they are not a claim of complete UI or chess-engine validation.
