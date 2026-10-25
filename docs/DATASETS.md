# Datasets — 0.26.4-beta

## Workflow

Open **Datasets** from the home menu. Import one or more PGN, PGN.ZST, PGN.GZ or ZIP files (PGN entries inside ZIP), or choose months from the live Lichess standard-rated catalog. The catalog shows approximate download size and game count; click to select, Ctrl-click to add/remove and Shift-click for a range. Downloads are checksummed against Lichess's SHA-256 list and recorded before transfer starts. The next transfer can overlap the current import; queue progress distinguishes downloaded from processed archives. Servers that ignore Range cause a clean restart, never an append to the wrong bytes.

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
- Format 4 uses independently Zstandard-compressed blocks of up to 4,096 records, with a bounded raw payload and adaptive row/column coding. A sparse block directory permits binary search without decompressing the entire tree. Readers cache at most 32 decoded blocks.
- Compressed sorted runs are the **temporary, disk-backed aggregation workspace**. They are deleted after success/cancellation. SQLite remains the normalized, filterable game store, but is no longer an index builder. The final `.cotidx` file contains no JSON, PGN tags, names, timestamps or engine evaluations.
- In-memory aggregation keys run-length-encode empty square pairs. The destination key is not stored per edge: source position plus packed move reconstructs it exactly. Final blocks retain fixed-key prefix coding.

Existing format-2/3/4 indexes remain readable. Rebuilding writes format 4, requiring 0.26.3 or later; keep an old compatible snapshot if testing a downgrade. Opening files and learning progress are unaffected.

### Why an index can exceed a game list

A compact game list needs roughly one two-byte move per ply and reconstructs
positions sequentially. Fast arbitrary-position lookup materializes a queryable
identity for each distinct position, plus outcome counts and outgoing moves.
This is redundant, derived information traded for fast lookup: fewer PGN fields
does not imply fewer bytes than a compressed sequential representation. Late-game
positions are often unique, so shared openings do not offset all that overhead.
The bounded 1,000-game example has 62,895 distinct positions and a 662,228-byte
index (about 10.5 bytes per position after compression), versus a 204,800-byte
normalized game store. This is a design trade-off, not an information-theoretic
requirement that every possible tree encoding must be this large.

The previous temporary database had to update counters in arbitrary order while retaining
exact identities. Its B-tree pages and row overhead are not compressed like the
final sorted blocks. In 0.26.2 each position owns a small binary adjacency payload:
there is no separate edge B-tree repeating its parent identity for every move.
This reduces measured workspace by about 45% on the same 1,000-game input.
The 0.26.3 streaming builder implements multi-pass external aggregation
instead and is the only builder as of 0.26.4; see the historical comparison below. No arbitrary truncation or hash collisions are
introduced to make the numbers appear smaller.

There is no unconditional 1:32 size guarantee relative to an already compressed Lichess download. Unique late-game sequences still contain information. The optional cutoff trades those continuations for a smaller index without changing retained outcome counts.

## Continuation cutoff and size forecast

**Minimum games per node** defaults to **0**, meaning no depth limit. With **5**, any position reached by at most five games becomes a leaf with its exact outcome counts. Its outgoing moves are omitted. Positions still reachable through a frequent transposition remain available. Pruning uses the complete aggregated dataset, not a sample.

**0 and 1 are different:** 0 disables pruning; 1 retains a singleton position's counters but removes its continuations. The default is intentionally unchanged.

For a current completed tree, the sidebar shows the actual file size and the recorded peak aggregation workspace, not an approximation. Older trees have no historical workspace measurement; the app says so rather than inventing one.

When the selected sources are unchanged and the minimum is increased, the app can measure a prospective smaller index using the complete stored counts, including transpositions. This read-only preview uses the actual final encoder and is limited to 100,000 retained positions / five seconds of traversal. No source file or current index is replaced. It cannot restore continuations omitted by an earlier, higher minimum.

For a new build, the forecast measures the encoder on a bounded stratified sample (up to 2,000 games) and a random half of that same sample. It extrapolates their observed size growth using a bounded power law, at the **same absolute cutoff** for both measurements. The pruned estimate is capped by the projected full-depth size. This produces an educated point estimate even without complete frequencies; sparse or unrepresentative samples can still be inaccurate. It does not scale each singleton's frequency up as though the entire archive repeated that game. Exact measurements from a suitable existing index take precedence.

Workspace is estimated separately: the larger of scaled measured sample peak and **2.25 times the projected full-depth size**. A small sample often fits one run and never merges, whereas a real merge temporarily holds both input runs and their replacement. Pruning only happens after full aggregation, so a smaller pruned output does not imply a similarly small workspace. This budget is a heuristic, not an upper bound or an allocation/reservation. Actual completed tree size and measured workspace replace estimates after the build.

The live working-file measurement includes this operation's compressed runs and output, not unrelated profile files or imported datasets. Merge reporting shows **pass/total passes**, **block group/total groups**, and consumed/total input entries in the current pass. Its ETA starts with the merge phase and excludes pauses. Future passes initially use current throughput and size; coalescing duplicate keys may revise the estimate downward. An activity ring remains visible through pruning/export, where a reliable phase ETA is unavailable. Aggregation ETA uses recent throughput. Filtering still uses the import-time random reservoir for estimates and applies filters to every game when creating a subset. Existing datasets keep their original sample size. A random maximum-game limit samples matching row IDs instead of retaining every selected game's full contents in RAM.

An exact full-depth position index may be larger than its normalized game store: rare late-game positions need queryable identities and counts, whereas a game store can encode just successive moves. Transpositions and shared openings help, but do not make every late-game position shared. Use the cutoff to remove rare continuations; the app does not silently truncate depth or fabricate a compression ratio.

## Filters and limits

Both players must satisfy rating limits. Lichess calls these Glicko-2 ratings, not FIDE Elo. 3500 represents an open upper bound. Results and termination categories are independent filters. A recognizable terminal board can be filtered separately, but `Termination "Normal"` cannot reliably distinguish resignation from draw agreement. Cheater bans cannot be inferred when the source does not report them. Missing clock/rating data is not invented. The sequence filter currently matches an exact mainline prefix from the standard initial position, not an arbitrary position reached by transposition.

The graph starts at the standard initial position. Imported custom-FEN games are retained and can contribute to position lookups, but custom roots do not yet have a separate root selector in the graph browser. This is a beta limitation.

The browser starts with the root expanded, expands one selected continuation per depth, stops repetition loops, and orders replies by frequency. A forced line is compacted only when there is no later fork anywhere in that line. One wide node represents the entire tail and displays at most five moves plus an ellipsis; there is no extra continuation node. Expand it to inspect all individual moves; the first expanded move keeps its collapse handle. Fit uses visible node widths rather than hidden depth. Column spacing is adjustable, and an existing selection is retained when still visible. Edge width represents reply share; winner-border width is a bounded frequency/sample-size heuristic, **not a statistical significance test**. The side panel shows the absolute total, shared board/evaluation renderer and outcome bar, with links to Custom Game and the generator.

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

### 0.26.2 bounded measurements

| Measurement | First 1,000 records | First 10,000 records |
| --- | ---: | ---: |
| Accepted games | 1,000 | 10,000 |
| Plies | 67,320 | 673,034 |
| Normalized game store | 204,800 bytes | 1,970,176 bytes |
| Full final index | 662,228 bytes | 6,428,497 bytes |
| Cutoff-5 final index | 6,421 bytes | 72,366 bytes |
| Full / cutoff positions | 62,895 / 672 | 607,280 / 7,266 |
| Peak aggregation workspace | 3,584,000 bytes | 34,385,920 bytes |
| Full-index build | 0.96 s | 6.28 s |
| 1,000 cached lookups | 3.18 ms | 2.96 ms |

The first-1,000 final sizes are identical to 0.26.1; its measured workspace was
6,520,832 bytes. These prefixes are not random samples and do not establish a
whole-archive ratio, an upper workspace bound or a guaranteed run time.

## Streaming compact index (default since 0.26.4)

Every new build uses this path; the beta switch and superseded SQL builder have
been removed. Existing formats 2/3 remain readable and can be explicitly rebuilt
as format 4; the active file is never silently converted. Apps older than 0.26.3
cannot read format 4. Previously completed snapshots are not automatically deleted.
The following benchmark tables compare the two implementations as they existed
in 0.26.3; they do not imply that the old builder remains selectable.

### Design and costs

1. Reconstruct legal positions in the selected number of workers. Normalize
   uncapturable en passant, retain castling/side-to-move, and count each position
   and distinct outgoing move once per game, just like the stable implementation.
2. Aggregate a bounded batch (at most 100,000 distinct positions / 300,000
   contributed edges), sort it, and write a compressed run. No temporary SQL
   position table or secondary edge index is created.
3. Merge at most 16 runs at a time with a priority queue, summing identical keys
   and move counters. Delete consumed runs only after their replacement is
   complete. Runs are already compressed; later rounds do not materialize a
   raw expanded position table.
4. For a nonzero minimum, traverse the complete graph with a disk-backed
   one-bit-per-position visited map and two alternating sequential frontier
   files. This handles cycles and transpositions exactly. It retains a boundary
   position's counters but omits its outgoing edges. The whole graph is not
   stored as an in-memory hash set.
5. Write independently addressable blocks (up to 4,096 positions, with a bounded
   raw payload). Each block uses whichever compressed representation is smaller:
   interleaved prefix-coded records or separated key/counter/edge columns. Keep
   15-bit moves and shared/pure-result counters. A sparse directory locates a
   block by binary search; only that block is decoded for a position lookup.
   A decoded block caches a flat key/offset slab and materializes only the
   requested adjacency list, rather than thousands of unused record objects.

For **n input plies**, not just number of games, sorting/merge work is O(n log n)
in the worst case with fixed-size chess keys and bounded chess move degree.
Reachability is O((V + E) log V) and V/E are bounded by input occurrences. This is
an algorithmic bound, not a prediction of elapsed time: disk contention,
compression, OS paging and chess-rule reconstruction still matter. Aggregation
and merge fan-in are bounded; directory metadata is O(V / block size), and the
on-disk visited map uses approximately V/8 bytes. Temporary disk peak includes
both old/new runs during a merge and the complete index during pruning. Input
datasets and prior completed snapshots are not counted as newly owned workspace.

A known move sequence is replayed to a position in O(sequence length), followed
by directory/within-block binary searches and one block decompression. Full
identities are retained: there is no hash-collision approximation and no loss
of transposition counts. Keeping only a move-prefix trie would omit these
position-wide statistics, so it is not used as a disguised space saving.

The first 10,000 records of the supplied February 2017 archive (673,034 plies,
607,280 positions; not a random sample) produced these local measurements:

| Measure | Stable builder | Streaming beta |
| --- | ---: | ---: |
| Full index | 6,428,497 B | 5,608,682 B |
| Full-build peak workspace | 34,316,288 B | 11,347,938 B |
| Full build | 6.62 s | 9.43 s |
| Cutoff-5 index | 72,366 B | 56,426 B |
| Cutoff-5 build | 6.00 s | 12.29 s |
| 500 different lookups, full-index p95 / maximum | 0.08 / 4.31 ms | 5.05 / 21.95 ms |

This trades slower construction/larger per-lookup decompression for about 67%
less workspace and 13% less full-index space in that bounded test. Timings are
machine/load dependent and the OS disk cache was not flushed. **The requested
two-second ceiling on a 10-million-game dataset has not been certified.**
Neither a universal compression factor nor a maximum saving is promised.

Final 0.26.3 measurements after lazy block decoding, using the first 100,000
records (6,680,290 plies; 5,810,734 full / 75,172 cutoff-5 positions):

| Measure | Stable builder | Streaming beta |
| --- | ---: | ---: |
| Full index | 61,471,970 B | 53,494,271 B |
| Full-build peak workspace | 327,626,752 B | 108,268,082 B |
| Full build | 74.52 s | 73.24 s |
| Cutoff-5 index | 775,949 B | 611,250 B |
| Cutoff-5 build | 72.19 s | 109.34 s |
| Full-index open | 4.04 ms | 0.37 ms |
| 500 different lookups, full-index p95 / maximum | 0.09 / 3.38 ms | 0.74 / 17.01 ms |

The normalized game store occupied 19,668,992 B. This confirms approximately
67% less workspace, 13% less full-index space and 21% less cutoff-5 space on
this corpus. The earlier 10,000-record timings predate lazy adjacency decoding.
These are bounded prefix tests, not ten-million-game or cold-disk guarantees.
Full local regression, engine-generation checks and a clean public-source build
passed. Synthetic CI tests exact counts/edges and cancellation, not noisy timing.

The activity ring freezes during pause and Resume is highlighted; pause is an
in-process safe-boundary pause, not a durable index-build checkpoint. Closing
with cancellation deletes only this build's temporary runs and partial output,
and retains the previous complete index and all imported datasets. Import and
download checkpoint behavior is unchanged. Confirmation buttons use the app's
language, independently of the Windows display language.

### 0.26.4 forecast spot check

On the same bounded 10,000-record prefix, a 1,000-game reservoir produced:

| Quantity | Forecast | Actual |
| --- | ---: | ---: |
| Full-depth index | 5,401,186 B | 5,608,682 B |
| Cutoff-5 index | 51,885 B | 56,426 B |
| Full-build peak workspace | 12,152,668 B | 11,347,056 B |

The cutoff-5 workspace forecast remained 12,152,668 B because the full merge
still precedes pruning. These are spot checks, not accuracy guarantees for other
archives or thresholds. The final index sizes match the preceding format-4
implementation; this update changes reporting/default selection, not its codec.

The tree browser also folds a forced **prefix before a later fork** into its
first move plus an ellipsis. Expand it to reveal the whole linear section;
the subsequent fork remains separately expandable. Terminal forced tails keep
their up-to-five-move wide preview. Opening Freeplay now offers **Game statistics**
beside Top 3, and immediately reflows history when Top 3 is toggled.
