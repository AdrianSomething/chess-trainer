# Engine Lab — experimental training add-on

Engine Lab is a local, optional environment for experimenting with chess policies. It is **not a pretrained competitive engine**. The normal application works without Python, PyTorch or NEAT-Python. No remote model service, credentials, downloaded weights or training telemetry are involved.

## Start on Windows

1. Open **Settings → General → Engine Lab**. Close unrelated game/generator windows first. Normal puzzle preparation resumes when you leave the lab.
2. Install Python and a compatible [CUDA-enabled PyTorch build](https://pytorch.org/get-started/locally/) for GPU training. Then install this directory's `requirements.txt`. A generic PyTorch installation is not necessarily CUDA-enabled. NEAT additionally uses `neat-python==1.1.0` on the CPU.
3. In **Setup**, select a method and **Minimal test template (3 rounds)** first. Choose CPU or CUDA, the Python executable and the number of displayed boards (0 disables board rendering). Expand task cards to edit parameters. The curve button accepts draggable or numerical points with linear interpolation and repeat/hold behavior.
4. **Training** shows run/round/task progress, elapsed and estimated remaining time, persistent model IDs, optional boards and a score graph. Solid lines/dots show the round's best score; dashed lines show the mean. Hover a point for its values. Independent runs have separate series.
5. **Results** retains the graph and report, can save a model to the local library, select it as the default, open a separate custom game, or continue with edited settings. Stockfish can never be deleted. Discard closes the result view but preserves experiment files for reproducibility.

Esc returns to the normal menu. Running experiments ask to stop first. Stop preserves completed round checkpoints; an unfinished round is rerun. If closing requires termination after the grace period, only the last committed checkpoint is resumable. Checkpoints, templates and experiment reports are in the app data directory's `EngineLab` folder; saved playable models are in `Engines`.

## Four minimal, editable templates

| Method | Starter file | What learns | Round graph |
| --- | --- | --- | --- |
| Evolutionary population | `minimal-population.json` | Fixed-topology neural weights, mutation and selection | Best/mean weighted task fitness |
| Backpropagation / teacher imitation | `minimal-backpropagation.json` | Policy/value predictions from Stockfish targets, AdamW | Negative mean cross-entropy plus value MSE; higher is better |
| Self-Play Reinforcement Learning | `minimal-selfplay.json` | On-policy actor-critic from its own game outcomes | Mean discounted terminal return from the acting side |
| NEAT | `minimal-neat.json` | CPU network topology and weights, species and innovation-aware crossover | Best/mean weighted task fitness |

These are functional smoke-sized starting points, not tuned recipes. Self-play scores alone do not establish strength: draws, move-limit penalties and changing opponents/rewards affect interpretation. Optional independent Stockfish/task measurements remain important. No full 100-round convergence or Elo claim is made.

```powershell
# Run from this add-on directory, using the complete application package as the host.
python train.py --host "C:\Apps\ChessOpeningTrainer\ChessOpeningTrainer.exe" --output "C:\Experiments\first" --config minimal-selfplay.json
python train.py --host "C:\Apps\ChessOpeningTrainer\ChessOpeningTrainer.exe" --output "C:\Experiments\first" --config minimal-selfplay.json --resume
python -m unittest discover -p "test_*.py"
```

All UI settings are plain JSON and available from code. `Config` supplies backwards-compatible defaults; `launch-config.json` records the UI configuration and each run's `config.json` records its resolved inputs. Multiple runs use separate directories/seeds. Resume adds the configured number of rounds to the selected run. Keep the original method and model architecture; tasks and subsequent-round schedules may change. Curve x coordinates are absolute round numbers on continuation.

## Models and memory

All three tensor models share 17 board planes (pieces, side to move, four castling rights), two 32-channel convolution layers, a 20,480-action policy head and a scalar value head. Hidden widths are defined in `models.py`; memory is used by actual trainable parameters, not padding.

| Profile | Hidden widths | FP32 weights | Intended use |
| --- | --- | --- | --- |
| `compact` | 128 | about 11 MiB | Fast tests; roughly 0.1 GiB in the earlier inference smoke configuration |
| `balanced-8gb` | 4096, 2048 | about 224 MiB | Larger experiments on an 8 GB card; estimated AdamW training footprint about 1.4 GiB |
| `backprop-8gb` | 15360, 15360, 4096 | about 1.54 GiB | Large backpropagation model sized for an 8 GB card |

The large profile was checked on an RTX 4060 Laptop GPU (8 GB), PyTorch 2.11/CUDA 13.0, with two complete FP32 AdamW updates at batch size 16. Peak PyTorch allocation was **6,648,979,456 bytes** and peak reservation **6,677,331,968 bytes (about 6.22 GiB)**. The conservative estimate including reserve is about 6.67 GiB. This is a bounded memory check, not a full long-run benchmark. Display/driver allocations and other programs are additional; available memory is checked before initialization. Larger batches may require reducing the model or batch size.

Weights cannot themselves occupy 6 GiB on this card during ordinary AdamW backpropagation: parameters, gradients and two optimizer moments already require roughly four times the parameter storage before activations. Fused CUDA AdamW and bounded batches avoid extra foreach-sized intermediates; see the [PyTorch AdamW documentation](https://docs.pytorch.org/docs/stable/generated/torch.optim.AdamW.html).

Population members are stored on disk and loaded one at a time onto the GPU. Only a single offspring is created in CPU memory at once. 100 balanced models need at least about 22 GiB of disk space; 100 large models would exceed 154 GiB before temporary/checkpoint space. The large profile is intended primarily for gradient training. AdamW continuation checkpoints also store optimizer moments and can be several times larger than `best.pt`.

NEAT has its own small CPU architecture: 69 normalized square/turn/castling inputs and a factorized 64-source + 64-destination + 5-promotion policy. NEAT-Python handles topology mutation, innovation history, species, crossover and reproduction. The UI exposes population size, node/connection addition probabilities and species distance; the fully resolved configuration is saved as `neat-config.ini`. See [NEAT-Python's configuration guide](https://neat-python.readthedocs.io/en/latest/config_essentials.html).

## Tasks, scores and rules

Population tasks can be repeated with independent settings and weights: balanced random tournaments, elimination tournaments with an optional second-loss bracket, Stockfish games, mate-in-N tasks and teacher move tasks. Integer game-count curves must evaluate to an even count for balanced random/Stockfish games; invalid configurations stop with an explicit message. Rounds can schedule population size, mutation, survivor fraction and parent-rank weighting. Survivors retain their IDs; offspring get new IDs.

Mate tasks support self-play, the current local dataset index or JSON/JSONL FEN records. Start/end depth-distribution curves interpolate across rounds. Tasks are checked by Stockfish and a finite discovery budget fails explicitly if the requested set cannot be found. A dataset source is resolved at launch; changing the active dataset later does not silently change that run.

Teacher imitation tasks schedule position counts, Stockfish depth, learning rate and batch size, with generated/initial/FEN/dataset/file sources. Policy cross-entropy and bounded value regression are optimized with AdamW. Both weights and optimizer/random states are checkpointed.

Self-play uses legal-mask sampling, discounted terminal rewards, a value baseline, an entropy term and a small differentiable penalty on illegal probability mass. Rollouts are collected without retaining an autograd graph, then recomputed in bounded minibatches. It is a minimal actor-critic example, **not AlphaZero, PPO or MCTS**. NEAT uses the same chess fitness tasks as the population method, with its own reproduction rules rather than tensor mutation.

The authoritative C# host retains castling, promotion, en-passant, repetition and move-limit state. The neural input intentionally omits en-passant, repetition history and the halfmove clock, so equal network inputs may have different legal masks. Only legal actions are executed. A 500-full-move cap means 1,000 plies. Terminal states never receive a fabricated continuation. See [PROTOCOL.md](PROTOCOL.md).

## Saved engines and independent evaluation

**Settings → Position evaluation → Evaluation engine** lists Stockfish and models saved by this lab. The choice applies to background board evaluation and automatic opponents; generator and puzzle validation continue to use Stockfish. Tensor models supply policy/value estimates without tree search; depth settings have no search meaning for these policies. NEAT supplies its evolved policy with a clearly identified **material-only evaluation**, not a trained centipawn estimator.

The result screen can benchmark against increasing Stockfish depths, measure decision latency or evaluate tactic tasks. Move-capped games are reported separately, never counted as successful draws. A depth comparison is not an Elo rating, and repeated deterministic positions are not independent strength evidence. Normal full-depth Stockfish play can take much longer than the background evaluation's time-limited searches.

## Files and extension points

- `config.py`, `curves.py`, `minimal-*.json`: configuration and schedules.
- `client.py`, `positions.py`: shared-rule host transport and position sources.
- `models.py`, `model.py`, `inference.py`: tensor architecture, encoding and playable inference.
- `schedule.py`, `arena.py`, `tasks.py`, `tactics.py`: pairings, concurrent games and fitness tasks.
- `evolution.py`, `population.py`: fixed-topology population training with disk-backed members.
- `distillation.py`, `selfplay.py`, `gradient_checkpoint.py`: gradient methods and resumable optimizer state.
- `neat_training.py`, `neat_policy.py`: topology evolution and portable data-only inference export.
- `result_tasks.py`: shared independent result measurements.

No training starts on module import. Tensor inference loads tensor-only weights. Portable NEAT engines are JSON, not executable Python; NEAT population checkpoints use the library's pickle format and are only resumed from locally generated experiment directories. Do not open untrusted checkpoints.

CI runs dependency-free schedule/portable-policy checks and the Windows app's synthetic regressions. Full GPU runs, large model allocations and live downloads remain local opt-in verification, not per-push Actions jobs.
