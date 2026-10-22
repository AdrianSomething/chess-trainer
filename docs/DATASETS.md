# Data sets — 0.26.0-beta

## Workflow

Open **Data sets** from the home menu. Import one or more PGN, PGN.ZST, PGN.GZ or ZIP files (PGN entries inside ZIP), or choose months from the live Lichess standard-rated catalog. The catalog shows approximate download size and game count. Downloads are sequential, checksummed against Lichess's SHA-256 list, and recorded before transfer starts. Close/pause leaves a partial file and queue; reopen Data sets and select **Resume download**. Servers that ignore Range cause a clean restart, never an append to the wrong bytes.

The app creates normalized, local datasets under the profile's `DataSets` directory. Original import files are unchanged. Rename datasets, activate any combination, or create a trimmed subset. A subset can replace the app-owned original only after successful creation. Deleting a dataset requires confirmation. Duplicate games in separately imported, overlapping datasets are not automatically deduplicated; activate non-overlapping sources when unbiased game counts matter.

Build the active datasets into a frequency tree. Changing the active selection or continuation cutoff does not modify the existing tree. An outdated-tree warning remains until rebuilding, or disappears if you restore the selection and cutoff used for the current snapshot. Cancelling a rebuild retains the previous tree. Old completed index snapshots are currently retained on disk; they are not automatically deleted while another view might still use them.

## What is retained?

The **normalized game store** keeps legal mainline moves, result, both ratings, initial time control and termination category. These are needed to change filters later. It omits player names, profile links, event headers, comments, evaluations, per-move clocks, SAN and move numbers. Standard initial FEN is implicit. Source games with no moves, unsupported variants or invalid moves are counted as skipped. ZIP entries are streamed, not extracted to arbitrary filesystem paths.

The **frequency index** keeps only position identities, played moves and four absolute outcome counters: White win, draw, Black win, unknown. A position or edge counts at most once per game. Move counters are excluded from identity; side to move, castling rights and legally available en passant remain significant. Transpositions share counts. Consequently, following several edges can cross move orders: these are **position frequencies**, not a claim that every displayed full sequence was played verbatim.

## Compact encoding

- A move uses 15 meaningful bits in a two-byte word: source square (6), destination square (6), promotion (3). Queens, rooks, bishops and knights are supported; ordinary moves, castling and en passant are reconstructed using the position. Text notation is only a presentation layer.
- Position keys are exact 35-byte board/state descriptions, not probabilistic hashes. Sorted adjacent keys share a prefix; only the changed suffix is written.
- Outcome counts use unsigned variable-length integers, so small counts do not occupy eight bytes each.
- Groups of up to 256 records are independently Zstandard-compressed. A sparse block directory permits binary search without decompressing the entire tree. Readers cache at most 32 decoded blocks.
- SQLite is a **temporary, disk-backed aggregation workspace**, not the final index format. It is deleted after success/cancellation. The final `.cotidx` file contains no JSON, PGN tags, names, timestamps or engine evaluations.

There is no unconditional 1:32 size guarantee relative to an already compressed Lichess download. Unique late-game sequences still contain information. The optional cutoff trades those continuations for a smaller index without changing retained outcome counts.

## Continuation cutoff and size forecast

**Stop continuations at N games or fewer** defaults to **0**, meaning no depth limit. With **5**, any position reached by at most five games becomes a leaf with its exact outcome counts. Its outgoing moves are omitted. Positions still reachable through a frequent transposition remain available. Pruning uses the complete aggregated dataset, not a sample.

The file-size forecast uses the actual encoder on a bounded, stratified sample (up to 2,000 games). It displays a range: observed-count and sample-scaled cutoffs give different estimates of rare tails. This is a rough extrapolation, not a confidence interval or storage promise. Temporary aggregation space is shown separately and can greatly exceed final size. Filtering uses the import-time random reservoir (default 1,000 games) for estimates, while subset creation applies the filter to every game. Existing datasets keep their original sample size. A random maximum-game limit samples matching row IDs instead of retaining every selected game's full contents in RAM.

## Filters and limits

Both players must satisfy rating limits. Lichess calls these Glicko-2 ratings, not FIDE Elo. 3500 represents an open upper bound. Results and termination categories are independent filters. A recognizable terminal board can be filtered separately, but `Termination "Normal"` cannot reliably distinguish resignation from draw agreement. Cheater bans cannot be inferred when the source does not report them. Missing clock/rating data is not invented. The sequence filter currently matches an exact mainline prefix from the standard initial position, not an arbitrary position reached by transposition.

The graph starts at the standard initial position. Imported custom-FEN games are retained and can contribute to position lookups, but custom roots do not yet have a separate root selector in the graph browser. This is a beta limitation.

The browser expands one selected continuation per depth, follows forced lines, stops repetition loops, and orders replies by frequency. Edge width represents reply share; winner-border width is a bounded frequency/sample-size heuristic, **not a statistical significance test**. The separate selection outline remains visible. The side panel uses the shared board/evaluation renderer and can open a position in Custom Game or preserve its sequence for the Opening Branch Generator.

## Validation performed locally

On 2026-10-22 the real January 2013 archive was downloaded once (17,761,302 bytes) and matched Lichess SHA-256 `aa40b3671fa3cf1072eb182892cd90b0e1e003a4a5943492f64b77e7f3fd1635`. This is an explicit manual integration check, not a recurring or CI download.

For the supplied 1,799,107,597-byte February 2017 archive, only the first 1,000 game records were used for the bounded benchmark: 995 legal non-empty games, 67,320 plies, approximately 0.5 seconds parsing. The full-depth compact index was 689,789 bytes (62,895 positions); cutoff 5 produced 6,510 bytes (672 positions). A thousand cached starting-position lookups took about 5.4 ms. Timings vary by machine and load. This is not a full-archive benchmark and is not used to claim an archive-wide compression ratio.

Offline checks cover all 20,480 source/target/promotion encodings, legal SAN roundtrips, imports, filtering, transpositions, threshold invalidation, exact terminal counts, cancellation, fake-HTTP resume/restart/checksum cases and form construction. No personal library or real AppData is used by tests. A full 1.8-GB end-to-end import remains outside this bounded validation.
