"""Editable experiment defaults; no training starts when this module is imported."""
from dataclasses import dataclass


@dataclass
class Config:
    mode: str = "evolution"
    population: int = 100
    rounds: int = 100
    peer_games: int = 10
    stockfish_games: int = 6
    tactics: int = 10
    max_moves: int = 500  # full moves, i.e. at most 1000 plies
    stockfish_depth: int = 1
    depth_every: int = 10
    cycle_rounds: int = 25
    min_survival: float = .05
    max_survival: float = .20
    high_mutation: float = .15
    low_mutation: float = .015
    illegal_penalty: float = .05
    survival_reward: float = .002
    win_reward: float = 100
    win_decay_per_ply: float = .01
    loss_reward: float = -30
    draw_reward: float = -5
    limit_penalty: float = 10
    tactic_reward: float = 20
    teacher_depth: int = 0  # optional per-move teacher feedback; 0 disables
    tactic_depth: int = 10
    tactic_attempts: int = 5000
    position_source: str = "initial"  # initial, generated, fen
    fen: str = ""
    record_thinking: bool = True
    visualize: bool = True
    seed: int = 2700
    device: str = "cuda"
    vram_gib: float = 6.0
    batch_size: int = 128
    distillation_positions: int = 1024
    distillation_depth: int = 8
    learning_rate: float = .0003
    benchmark_max_depth: int = 16
    benchmark_games: int = 6

    def validate(self):
        if not 2 <= self.population <= 1000:
            raise ValueError("Population must be between 2 and 1000.")
        if self.peer_games % 2 or self.stockfish_games % 2 or self.benchmark_games % 2:
            raise ValueError("Game counts must be even for equal White/Black allocation.")
        if min(self.peer_games, self.stockfish_games, self.tactics) < 0 or self.benchmark_games < 2:
            raise ValueError("Invalid game counts.")
        if self.population * (self.peer_games // 2 + self.stockfish_games) > 4096:
            raise ValueError("At most 4096 simultaneously scheduled games are supported.")
        if self.rounds < 1 or self.max_moves < 1 or self.cycle_rounds < 2 or self.depth_every < 1:
            raise ValueError("Round and move limits must be positive.")
        if not 0 < self.min_survival <= self.max_survival <= 1:
            raise ValueError("Invalid survivor fractions.")
        if self.win_decay_per_ply <= self.survival_reward:
            raise ValueError("Win decay must outweigh survival rewards.")
        if self.position_source == "fen" and not self.fen:
            raise ValueError("A starting FEN is required.")
        if self.batch_size < 1 or self.batch_size > 256 or not 0 < self.vram_gib <= 6:
            raise ValueError("Batch size: 1–256; GPU allocation budget: at most 6 GiB.")
