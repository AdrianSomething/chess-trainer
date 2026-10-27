"""Compact policy/value network. Only one population member resides on the GPU."""
import torch
from torch import nn

PIECES = "PNBRQKpnbrqk"
ACTIONS = 64 * 64 * 5  # source, destination, no promotion/Q/R/B/N


def encode(states, device):
    planes = torch.zeros((len(states), 17, 8, 8), dtype=torch.float32)
    for batch, state in enumerate(states):
        for square, piece in enumerate(state["board"]):
            if piece in PIECES:
                planes[batch, PIECES.index(piece), square // 8, square % 8] = 1
        planes[batch, 12].fill_(int(state["white"]))
        for index, right in enumerate("KQkq"):
            planes[batch, 13 + index].fill_(int(right in state["castling"]))
    return planes.to(device)


from models import ChessNet, requirements


class Policy:
    def __init__(self, config):
        torch.set_num_threads(2)
        self.device = torch.device("cuda:0" if config.device == "cuda" else config.device)
        if self.device.type == "cuda":
            if not torch.cuda.is_available():
                raise RuntimeError("CUDA PyTorch is required; use device='cpu' for a small test.")
            total = torch.cuda.get_device_properties(self.device).total_memory
            # Reserve headroom for the CUDA context/driver outside PyTorch's allocator.
            fraction = min(.90, max(.05, (config.vram_gib - .5) * 1024**3 / total))
            torch.cuda.set_per_process_memory_fraction(fraction, self.device)
        profile = getattr(config,"model","compact")
        if self.device.type == "cuda":
            free, _ = torch.cuda.mem_get_info(self.device)
            need=requirements(profile,config.mode in ("distillation","selfplay"))["gpu_bytes"]
            if free < need: raise RuntimeError(f"This model needs approximately {need/1024**3:.1f} GiB free GPU memory; available: {free/1024**3:.1f} GiB. Close GPU applications or select a smaller model.")
        self.net = ChessNet(profile).to(self.device)
        self.config = config
        self.loaded = None

    def load(self, identity, weights):
        if self.loaded != identity:
            if isinstance(weights, (str, __import__("pathlib").Path)):
                weights=torch.load(weights,map_location="cpu",weights_only=True,mmap=True)
            self.net.load_state_dict(weights)
            self.loaded = identity
        self.net.eval()

    @torch.inference_mode()
    def choose(self, states):
        logits, _ = self.net(encode(states, self.device))
        results = []
        for row, state in zip(logits, states):
            legal = state["legal"]
            if not legal:
                raise ValueError("Terminal positions cannot be played.")
            indices = torch.tensor([m["index"] for m in legal], device=self.device)
            choice = legal[int(row[indices].argmax().item())]
            # The unmasked top preference determines the small illegality penalty.
            illegal = int(row.argmax().item()) not in {m["index"] for m in legal}
            results.append((choice["uci"], illegal))
        return results

    def cpu_weights(self):
        return {name: value.detach().to("cpu",copy=True) for name, value in self.net.state_dict().items()}
