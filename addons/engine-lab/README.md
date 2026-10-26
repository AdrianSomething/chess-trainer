# Engine Lab (experimental add-on)

Engine Lab is an optional, local research scaffold for training and evaluating a small chess policy. It is **not a pretrained competitive engine**. The desktop application and all normal training modes work without Python or PyTorch.

## Start on Windows

1. Use the complete Windows application package, including its Stockfish engine. In **Settings → General**, open **Engine Lab** below the guided overview button. Close other training/generator windows first; their work is never silently discarded.
2. Install Python and a CUDA-enabled PyTorch build appropriate for your driver using the [official PyTorch installer](https://pytorch.org/get-started/locally/). The add-on was exercised with Python 3.12 and PyTorch 2.11 on an RTX 4060 Laptop GPU. `requirements.txt` states the Python dependency; a generic package installation does not guarantee CUDA support.
3. Enter the Python executable in the laboratory. Choose population training, Stockfish distillation, a saved-model benchmark, or the standalone position laboratory. Each parameter has a tooltip. The position laboratory requires no Python.
4. Start a small experiment before a long run. **Esc** returns to the normal menu; running experiments first ask to stop. **Continue** adds the selected number of rounds, using the last saved round checkpoint.

The normal puzzle refill and background board analysis are suspended while the laboratory is open and restarted on return. The training host does not create normal app windows, history, dataset views or background analysis. A teacher engine is started only when requested. A visual monitor is optional; it paints at most four updates per second.

For a bounded command-line smoke run from this directory:

```powershell
python train.py --host "C:\Apps\ChessOpeningTrainer\ChessOpeningTrainer.exe" --output "C:\Experiments\smoke" --smoke
python train.py --host "C:\Apps\ChessOpeningTrainer\ChessOpeningTrainer.exe" --output "C:\Experiments\smoke" --smoke --resume
```

Use a separate experiment folder. No training starts on import. To customize all parameters, edit the defaults in `config.py`, or pass a JSON file with selected overrides using `--config`. The UI saves its selected overrides as `launch-config.json`. Python, downloaded dependencies, model weights and private experiments are not bundled into the source release.

## Model and rule boundary

The model has two 32-channel convolution layers, a 128-unit hidden layer, a 20,480-action policy head and a scalar value head. Its 17 input planes contain pieces, side to move and four castling rights. Board squares run from a1 to h8. Action indices encode source, destination and five promotion choices; see [PROTOCOL.md](PROTOCOL.md).

As requested, en-passant availability, repetition history and the halfmove clock are not neural inputs. The authoritative C# host retains this state and supplies legal actions. Thus identical neural inputs can have different legal masks. The highest-ranked legal action is played; a small fitness penalty applies when the raw highest-ranked action was illegal. Castling, en passant, promotion, checkmate and draws use the application's shared rules. A limit of 500 full moves means 1,000 plies.

Only one member's network resides on the GPU. Other population weights remain on the CPU; ready positions for the same member are batched to reduce GPU model switches. The allocator budget is at most 6 GiB, with 0.5 GiB reserved for overhead. This limits PyTorch allocation, not every driver allocation or other applications' GPU use; see [PyTorch's allocator documentation](https://docs.pytorch.org/docs/2.11/generated/torch.cuda.memory.set_per_process_memory_fraction.html). The compact smoke test used about 78 MiB of reserved PyTorch GPU memory, not a measurement of a complete 100-member experiment.

## Default population experiment

- 100 members and 100 rounds. Each member plays 10 randomly paired peer games (five with each color) and six Stockfish games (three with each color). This is a balanced sampled tournament, **not all 4,950 pairings**. Repeated opponents are possible.
- Stockfish starts at depth 1 and gains one depth every ten rounds. Ten fresh mate-in-1–5 tasks are found through varied engine self-play for each round and shared by every member. Finding too few within the attempt budget stops with an explicit error instead of silently ranking an incomplete task set.
- Survival earns 0.002 per ply; a win earns 100 minus 0.01 per game ply, a loss −30, a draw −5, and reaching the move cap costs another 10. Illegal top preferences cost 0.05; solved mate tasks earn 20. Fast wins therefore outweigh passive survival. These are editable experimental rewards, not validated training-quality claims.
- A 25-round cycle increases survival from 5% to 20% and reduces mutation from 0.15 to 0.015, holding the final values for the last few rounds. Survivors are retained; other members are mutations of them.
- After training, the current best member plays balanced games against increasing Stockfish depths. The report is a **depth comparison, not an Elo estimate**. Capped games are reported separately and never counted as successful draws. Deterministic repeated starts are not independent strength evidence.

Evolution stores atomic `checkpoint.pt`/`best.pt` files, configuration, round history and a final JSON report. Stopping halfway through a round preserves the last complete checkpoint; that unfinished round is rerun. Resume requires the same population size. Stockfish depth comparisons and self-play can be expensive. No full 100-round strength/convergence claim is made.

## Other modes and extension points

- **Position laboratory:** independent games, generated legal positions or supplied FEN, selectable visible instance, direct position/legality responses, optional per-move teacher feedback.
- **Distillation:** generates shared-rule positions, records Stockfish policy/value targets, trains policy cross-entropy and value regression with AdamW, and saves weights. Continue reloads weights; optimizer state and a partially completed epoch are not restored.
- **Benchmark:** evaluates `best.pt` from the selected experiment with increasing Stockfish depth. It does not mutate the model.

`client.py` owns transport; `model.py` owns encoding/inference; `schedule.py` owns pairing, fitness and selection; `arena.py` owns concurrent games; `tactics.py` owns task discovery; `evolution.py` and `distillation.py` own training. The JSON-lines protocol can be used without this example model. Replace the policy or training loop without changing WinForms or chess rules. Commands are processed sequentially per host; independent game states allow batched inference, not simultaneous writes to one game.

Run dependency-free schedule checks with `python -m unittest discover -p "test_*.py"`. Windows CI also checks protocol-domain rules and keyboard/generator integration; it never installs CUDA or starts full training.
