# Data sets — 0.26.1-beta

## Workflow

Open **Data sets** from the home menu. Import one or more PGN, PGN.ZST, PGN.GZ or ZIP files (PGN entries inside ZIP), or choose months from the live Lichess standard-rated catalog. The catalog shows approximate download size and game count; click to select, Ctrl-click to add/remove and Shift-click for a range. Downloads are checksummed against Lichess's SHA-256 list and recorded before transfer starts. The next transfer can overlap the current import; queue progress distinguishes downloaded from processed archives. Servers that ignore Range cause a clean restart, never an append to the wrong bytes.

**Pause** holds the same operation in memory; **Resume** continues it. **Cancel** discards unfinished work, leaving existing datasets and the previous tree intact. **Stop and save** publishes the games already processed (or keeps a partial download for later). Closing the window retains download state and committed import checkpoints. Reopen Data sets and choose **Resume download/import**. Compressed imports replay the source stream to their saved record boundary, but do not revalidate or duplicate saved games. Filtering and indexing are paused only while the application remains open; closing discards those unfinished operations. Time estimates become useful only after work has been measured and exclude deliberate pauses.

The app creates normalized, local datasets under the profile's `DataSets` directory; **Open dataset folder** opens that location directly. Existing data is not silently moved. Original import files are unchanged. Double-click a name and confirm with Enter or the checkmark; clicking elsewhere cancels the rename. Bookmark datasets, activate any combination, or create a trimmed subset. A separately kept subset is initially inactive. A subset can replace the app-owned original only after successful creation. Deleting a dataset requires confirmation. Duplicate games in separately imported, overlapping datasets are not automatically deduplicated; activate non-overlapping sources when unbiased game counts matter.

Build the active datasets into a frequency tree. Changing the active selection or continuation cutoff does not modify the existing tree. An outdated-tree warning remains until rebuilding, or disappears if you restore the selection and cutoff used for the current snapshot. Cancelling a rebuild retains the previous tree. Old completed index snapshots are currently retained on disk; they are not automatically deleted while another view might still use them.

## What is retained?

The **normalized game store** keeps legal mainline moves, result, both ratings, initial time control and termination category. These are needed to change filters later. It omits player names, profile links, event headers, comments, evaluations, per-move clocks, SAN and move numbers. Standard initial FEN is implicit. Valid games with no moves are accepted (for example, an immediate timeout). Unsupported variants or invalid moves are counted as skipped with reason totals available on the dataset card. Previous imports cannot retroactively recover omitted games or their reasons: reimport the source if needed. ZIP entries are streamed, not extracted to arbitrary filesystem paths.

The **frequency index** keeps only position identities, played moves and four absolute outcome counters: White win, draw, Black win, unknown. A position or edge counts at most once per game. Move counters are excluded from identity; side to move, castling rights and legally available en passant remain significant. Transpositions share counts. Consequently, following several edges can cross move orders: these are **position frequencies**, not a claim that every displayed full sequence was played verbatim.

## Compact encoding

- A move uses 15 meaningful bits in a two-byte word: source square (6), destination square (6), promotion (3). Queens, rooks, bishops and knights are supported; ordinary moves, castling and en passant are reconstructed using the position. Text notation is only a presentation layer.
- Position keys are exact 35-byte board/state descriptions, not probabilistic hashes. Sorted adjacent keys share a prefix; only the changed suffix is written.
- Outcome counts use unsigned variable-length integers, so small counts do not occupy eight bytes each.
- In format 3, a single-outcome position uses one tagged counter instead of four; an edge whose counters equal its parent's refers to those counts using the unused high move bit. Mixed results retain every exact counter.
- Groups of up to 256 records are independently Zstandard-compressed. A sparse block directory permits binary search without decompressing the entire tree. Readers cache at most 32 decoded blocks.
- SQLite is a **temporary, disk-backed aggregation workspace**, not the final index format. It is deleted after success/cancellation. The final `.cotidx` file contains no JSON, PGN tags, names, timestamps or engine evaluations.
- Temporary position keys run-length-encode empty square pairs. Inverse run lengths preserve the original key's lexical order, avoiding a second dataset-sized sort before export. The destination key is not stored per edge: source position plus packed move reconstructs it exactly. The final index deliberately retains fixed-key prefix coding: two superficially shorter alternative encodings compressed worse in the bounded benchmark.

Existing format-2 indexes remain readable. Rebuilding writes format 3. Version 0.26.0 cannot read the newer index; keep its old snapshot if testing a downgrade. Opening files and learning progress are unaffected.

There is no unconditional 1:32 size guarantee relative to an already compressed Lichess download. Unique late-game sequences still contain information. The optional cutoff trades those continuations for a smaller index without changing retained outcome counts.

## Continuation cutoff and size forecast

**Minimum games per node** defaults to **0**, meaning no depth limit. With **5**, any position reached by at most five games becomes a leaf with its exact outcome counts. Its outgoing moves are omitted. Positions still reachable through a frequent transposition remain available. Pruning uses the complete aggregated dataset, not a sample.

The file-size forecast uses the actual encoder on a bounded, stratified sample (up to 2,000 games). Observed-count and sample-scaled cutoffs give different estimates of rare tails; a range is shown only when its rounded endpoints differ. This is a rough extrapolation, not a confidence interval or storage promise. Temporary aggregation space is measured in the sample run instead of assuming a fixed cost per ply, and shown separately. The live working-file measurement includes the owned SQLite database and journal/WAL files, not unrelated application/profile files. Filtering uses the import-time random reservoir (default 1,000 games) for estimates, while subset creation applies the filter to every game. Existing datasets keep their original sample size. A random maximum-game limit samples matching row IDs instead of retaining every selected game's full contents in RAM.

An exact full-depth position index may be larger than its normalized game store: rare late-game positions need queryable identities and counts, whereas a game store can encode just successive moves. Transpositions and shared openings help, but do not make every late-game position shared. Use the cutoff to remove rare continuations; the app does not silently truncate depth or fabricate a compression ratio.

## Filters and limits

Both players must satisfy rating limits. Lichess calls these Glicko-2 ratings, not FIDE Elo. 3500 represents an open upper bound. Results and termination categories are independent filters. A recognizable terminal board can be filtered separately, but `Termination "Normal"` cannot reliably distinguish resignation from draw agreement. Cheater bans cannot be inferred when the source does not report them. Missing clock/rating data is not invented. The sequence filter currently matches an exact mainline prefix from the standard initial position, not an arbitrary position reached by transposition.

The graph starts at the standard initial position. Imported custom-FEN games are retained and can contribute to position lookups, but custom roots do not yet have a separate root selector in the graph browser. This is a beta limitation.

The browser starts with the root expanded, expands one selected continuation per depth, compacts forced lines into up to five moves per wider node, stops repetition loops, and orders replies by frequency. Expand a compact line to inspect individual moves. Column spacing is adjustable, and an existing selection is retained when it is still visible. Edge width represents reply share; winner-border width is a bounded frequency/sample-size heuristic, **not a statistical significance test**. A thin opposite-color outer stroke keeps white/black borders visible across themes; the separate selection outline remains visible. The side panel uses the shared board/evaluation renderer and can open a position in Custom Game or preserve its sequence for the Opening Branch Generator.

**Game statistics** can also be enabled in learning, Custom Game and game analysis; the generator displays the strip below its board. Learning hides it until the answer is known. Lookups run in the background with bounded block caches and stale-result protection; they do not start additional engines.

## Validation performed locally

On 2026-10-22 the real January 2013 archive was downloaded once (17,761,302 bytes) and matched Lichess SHA-256 `aa40b3671fa3cf1072eb182892cd90b0e1e003a4a5943492f64b77e7f3fd1635`. This is an explicit manual integration check, not a recurring or CI download.

For the supplied 1,799,107,597-byte February 2017 archive, only the first 1,000 game records were used for bounded benchmarks (not a random sample):

| Measurement | 0.26.0 | 0.26.1 |
| --- | ---: | ---: |
| Accepted records | 995 | 1,000 (including five valid zero-move games) |
| Plies | 67,320 | 67,320 |
| Full-depth final index | 689,789 bytes | 662,228 bytes |
| Index with cutoff 5 | 6,510 bytes | 6,421 bytes |
| Retained positions: full / cutoff 5 | 62,895 / 672 | 62,895 / 672 |
| Measured owned aggregation workspace | Not recorded | 6,520,832 bytes |

The 2026-10-24 run parsed the prefix in about 0.62 seconds, built the full index in 1.52 seconds and performed 1,000 cached starting-position lookups in 3.31 ms. Timings vary by machine and load. This is not a full-archive benchmark and is not used to claim an archive-wide compression ratio. Alternative bitmap/RLE final-key encodings measured 824,268 and 904,231 bytes and were rejected in favor of the smaller prefix-coded format.

Offline checks cover all 20,480 source/target/promotion encodings, legal SAN roundtrips, imports, durable checkpoint/resume without duplicates, stop-and-save, filtering, transpositions, key sort order, old format-2/3 reading, threshold invalidation, exact terminal counts, cancellation, fake-HTTP resume/restart/checksum cases and form construction. No personal library or real AppData is used by tests. A full 1.8-GB end-to-end import remains outside this bounded validation.
