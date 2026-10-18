# Brief for assisted repertoire research

Copy this brief and the FORMAT.md document into your research tool. Fill in the bracketed fields; do not include personal games or credentials unless you intentionally choose to share them.

## Task

Create a legal opening repertoire for [opening] from [white/black] perspective, starting at [standard position / FEN], with approximately [number] distinct learner decisions and [depth] half-moves of coverage.

Research trustworthy opening sources and frequent opponent responses where data is available. Distinguish empirical frequency, engine preference and your own inference. Give source links and access dates; do not invent database counts or quote protected annotations.

Return a JSON file conforming to FORMAT.md. Use stable, non-personal IDs. Prescribe exactly one learner move at every nonterminal learner position; include multiple meaningful opponent replies. Merge genuine transpositions only when position identity and continuation agree. Do not introduce cycles.

Check every move for legality. If you cannot execute a chess validator or engine, explicitly say which checks were not performed. Do not label unverified output as engine-checked. Include brief original explanations of plans and common mistakes.

## Human acceptance checklist

Verify the learner perspective, legality, coverage, source quality and chosen responses in the application. Review surprising moves with an engine. Preserve the research record separately; do not put private prompts into opening metadata.
